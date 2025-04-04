﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace Imperial_Commander_Editor
{
	public class FileManager
	{
		public FileManager() { }

		/// <summary>
		/// Checks if the base save folder exists (Documents/ImperialCommander) and creates it if not
		/// </summary>
		private static bool CreateBaseDirectory()
		{
			string basePath = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments ), "ImperialCommander" );

			if ( !Directory.Exists( basePath ) )
			{
				var di = Directory.CreateDirectory( basePath );
				if ( di == null )
				{
					MessageBox.Show( "Could not create the base project folder.\r\nTried to create: " + basePath, "App Exception", MessageBoxButton.OK, MessageBoxImage.Error );
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// saves a mission and its default translation to base project folder
		/// </summary>
		public static bool Save( Mission mission, bool saveAs )
		{
			string basePath = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments ), "ImperialCommander" );

			if ( !CreateBaseDirectory() )
				return false;

			string filePath;//full path to the file, including filename
			if ( saveAs || string.IsNullOrEmpty( mission.fileName ) )
			{
				SaveFileDialog saveFileDialog = new();
				saveFileDialog.DefaultExt = ".json";
				saveFileDialog.Title = "Save Mission";
				saveFileDialog.Filter = "Mission File (*.json)|*.json";
				saveFileDialog.InitialDirectory = basePath;
				if ( saveFileDialog.ShowDialog() == true )
				{
					filePath = saveFileDialog.FileName;
					mission.fullPathToFile = filePath;
					//mission.relativePath = Path.GetRelativePath( basePath, new DirectoryInfo( filePath ).FullName );
				}
				else
					return false;
			}
			else
				filePath = mission.fullPathToFile;

			FileInfo fi = new( filePath );
			mission.fileName = fi.Name;
			mission.fullPathToFile = filePath;
			mission.saveDate = DateTime.Now.ToString( "M/d/yyyy" );
			mission.timeTicks = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();//DateTime.Now.Ticks
			mission.fileVersion = Utils.formatVersion;

			string output = JsonConvert.SerializeObject( mission, Formatting.Indented );
			try
			{
				using ( var stream = File.CreateText( mission.fullPathToFile ) )
				{
					stream.Write( output );
				}
				//add saved file to MRU list
				AddSaveMRU( mission.fullPathToFile );
			}
			catch ( Exception e )
			{
				MessageBox.Show( "Could not save the Mission.\r\n\r\nException:\r\n" + e.Message, "App Exception", MessageBoxButton.OK, MessageBoxImage.Error );
				return false;
			}

			//now save the default translation for the Mission
			try
			{
				//strip default language out of the Mission and save it
				TranslatedMission translation = TranslatedMission.CreateTranslation( mission );
				var lang = mission.languageID.Split( '(', ')' )[1];
				string tfname = $"{mission.fullPathToFile.Substring( 0, mission.fullPathToFile.Length - 5 )}_{lang}.json";
				//serialize to json
				output = JsonConvert.SerializeObject( translation, Formatting.Indented );
				//save it
				using ( var stream = File.CreateText( tfname ) )
				{
					stream.Write( output );
				}
			}
			catch ( Exception e )
			{
				MessageBox.Show( "Could not save the Mission's translation.\r\n\r\nException:\r\n" + e.Message, "App Exception", MessageBoxButton.OK, MessageBoxImage.Error );
				return false;
			}
			return true;
		}

		/// <summary>
		/// Loads a mission from its .json.
		/// Supply the FULL PATH with the filename
		/// </summary>
		public static Mission LoadMission( string filename )
		{
			string json = "";
			string basePath = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments ), "ImperialCommander" );

			try
			{
				using ( StreamReader sr = new( filename ) )
				{
					json = sr.ReadToEnd();
				}

				//sanity check
				if ( !json.Contains( "missionGUID" ) )
					throw new( "File doesn't appear to be a Mission." );

				var m = JsonConvert.DeserializeObject<Mission>( json );
				//overwrite fileName, relativePath and fileVersion properties so they are up-to-date
				FileInfo fi = new FileInfo( filename );
				m.fileName = fi.Name;
				m.fullPathToFile = fi.FullName;
				m.fileVersion = Utils.formatVersion;

				//convert all local triggers/events to global versions
				int eCount = 0;
				int tCount = 0;
				foreach ( MapSection item in m.mapSections )
				{
					foreach ( var evnt in item.missionEvents )
					{
						if ( evnt.GUID != Guid.Empty )
						{
							m.globalEvents.Add( evnt );
							eCount++;
						}
					}
					foreach ( var trig in item.triggers )
					{
						if ( trig.GUID != Guid.Empty )
						{
							m.globalTriggers.Add( trig );
							tCount++;
						}
					}
					//clear them so they aren't used
					item.missionEvents.Clear();
					item.triggers.Clear();
				}

				if ( eCount > 0 || tCount > 0 )
					Utils.Log( $"LoadMission()::Converted [{eCount}] LOCAL Events and [{tCount}] LOCAL Triggers to GLOBAL" );

				//add loaded file to MRU list
				AddSaveMRU( m.fullPathToFile );

				return m;
			}
			catch ( Exception e )
			{
				MessageBox.Show( "Could not load the Mission.\r\n\r\nException:\r\n" + e.Message, "App Exception", MessageBoxButton.OK, MessageBoxImage.Error );
				return null;
			}
		}

		public static Mission LoadMissionFromString( string json )
		{
			//called from CreateProjectItem() to show MRU items on Project window

			//make sure it's a mission, simple check for a property in the text
			if ( !json.Contains( "missionGUID" ) )
				return null;

			try
			{
				var m = JsonConvert.DeserializeObject<Mission>( json );
				//Utils.Log( "LoadMissionFromString: " + m.missionProperties.missionID );
				return m;
			}
			catch ( Exception e )
			{
				MessageBox.Show( "LoadMissionFromString()::Could not load the Mission.\n\nException:\n" + e.Message, "App Exception", MessageBoxButton.OK, MessageBoxImage.Error );
				return null;
			}
		}

		/// <summary>
		/// "filename" is a full path, returns null on failure
		/// </summary>
		public static ProjectItem CreateProjectItem( string filename )
		{
			//called from GetMRUProjects() to populate project list
			ProjectItem projectItem = new ProjectItem();

			if ( !File.Exists( filename ) )
				return null;

			var mission = LoadMissionFromString( File.ReadAllText( filename ) );
			if ( mission != null )
			{
				projectItem.fullPathWithFilename = filename;
				projectItem.fileName = new FileInfo( filename ).Name;
				projectItem.Title = mission.missionProperties.missionName;
				projectItem.Date = mission.saveDate;
				projectItem.fileVersion = mission.fileVersion;
				projectItem.timeTicks = mission.timeTicks;
				projectItem.Description = mission.missionProperties.missionDescription;
			}
			else
				return null;

			return projectItem;
		}

		/// <summary>
		/// Builds and returns ProjectItems from MRU list
		/// </summary>
		public static IEnumerable<ProjectItem> GetMRUProjects()
		{
			string basePath = Path.Combine( AppDomain.CurrentDomain.BaseDirectory, "ICEditor_MRU.txt" );

			try
			{
				if ( File.Exists( basePath ) )
				{
					List<ProjectItem> items = new();
					var mru = File.ReadAllLines( basePath ).ToList();
					//validate each file exists
					mru = mru.Where( x => File.Exists( x ) ).ToList();
					//overwrite the MRU in case it changed
					using ( TextWriter fs = new StreamWriter( basePath, false ) )
					{
						mru.ForEach( x => fs.WriteLine( x ) );
					}

					foreach ( string line in mru )
					{
						var pi = CreateProjectItem( line );
						if ( pi != null )
							items.Add( pi );
					}
					return items;
				}
				else
					return new List<ProjectItem>();
			}
			catch ( Exception )
			{
				return null;
			}
		}

		/// <summary>
		/// When saving or loading a Mission, add it to the MRU, then save the MRU list
		/// </summary>
		public static void AddSaveMRU( string fullPath )
		{
			string basePath = Path.Combine( AppDomain.CurrentDomain.BaseDirectory, "ICEditor_MRU.txt" );
			List<string> mru = new();

			if ( File.Exists( basePath ) )
			{
				mru = File.ReadAllLines( basePath ).ToList();
				//validate each file exists
				mru = mru.Where( x => File.Exists( x ) ).ToList();
			}

			//check if this file path already exists in the current MRU
			//add it to the top if it's not in the list
			if ( !mru.Any( x => x == fullPath ) )
				mru.Insert( 0, fullPath );
			else
			{
				int idx = mru.IndexOf( fullPath );
				mru.RemoveAt( idx );
				mru.Insert( 0, fullPath );
			}
			//overwrite the MRU
			using ( TextWriter fs = new StreamWriter( basePath, false ) )
			{
				mru.ForEach( x => fs.WriteLine( x ) );
			}
		}

		public static void RemoveFromMRU( string fullPath )
		{
			string basePath = Path.Combine( AppDomain.CurrentDomain.BaseDirectory, "ICEditor_MRU.txt" );

			if ( File.Exists( basePath ) )
			{
				List<string> mru = File.ReadAllLines( basePath ).ToList();
				for ( int i = mru.Count - 1; i >= 0; i-- )
				{
					if ( mru[i] == fullPath )
						mru.RemoveAt( i );
				}
				//overwrite the MRU
				using ( TextWriter fs = new StreamWriter( basePath, false ) )
				{
					mru.ForEach( x => fs.WriteLine( x ) );
				}
			}
		}

		public static void ClearMRU()
		{
			string basePath = Path.Combine( AppDomain.CurrentDomain.BaseDirectory, "ICEditor_MRU.txt" );
			File.Delete( basePath );
		}

		/// <summary>
		/// Load a Resource asset embedded in the app
		/// </summary>
		public static T LoadAsset<T>( string assetName )
		{
			try
			{
				string json = "";
				var assembly = Assembly.GetExecutingAssembly();
				var resourceName = assembly.GetManifestResourceNames().Single( str => str.EndsWith( assetName ) );
				using ( Stream stream = assembly.GetManifestResourceStream( resourceName ) )
				using ( StreamReader reader = new StreamReader( stream ) )
				{
					json = reader.ReadToEnd();
				}

				return JsonConvert.DeserializeObject<T>( json );
			}
			catch ( JsonReaderException e )
			{
				Utils.Log( $"FileManager::LoadData() ERROR:\r\nError parsing {assetName}" );
				Utils.Log( e.Message );
				throw new Exception( $"FileManager::LoadData() ERROR:\r\nError parsing {assetName}" );
			}
		}

		/// <summary>
		/// Handles full Toon export experience, returns bool for success
		/// </summary>
		public static bool ExportCharacter( CustomToon toon )
		{
			string basePath = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments ), "ImperialCommander" );

			if ( !CreateBaseDirectory() )
				return false;

			string filePath;//full path to the file, including filename
			SaveFileDialog saveFileDialog = new();
			saveFileDialog.DefaultExt = ".json";
			saveFileDialog.Title = "Export Custom Character";
			saveFileDialog.Filter = "Character File (*.json)|*.json";
			saveFileDialog.InitialDirectory = basePath;
			if ( saveFileDialog.ShowDialog() == true )
			{
				filePath = saveFileDialog.FileName;
			}
			else
				return false;

			string output = JsonConvert.SerializeObject( toon, Formatting.Indented );
			try
			{
				using ( var stream = File.CreateText( filePath ) )
				{
					stream.Write( output );
				}
			}
			catch ( Exception e )
			{
				MessageBox.Show( "Could not Export the Character.\r\n\r\nException:\r\n" + e.Message, "App Exception", MessageBoxButton.OK, MessageBoxImage.Error );
				return false;
			}

			return true;
		}

		public static CustomToon ImportCharacter()
		{
			string basePath = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments ), "ImperialCommander" );

			CreateBaseDirectory();

			OpenFileDialog openFileDialog = new();
			openFileDialog.DefaultExt = ".json";
			openFileDialog.Title = "Import Custom Character";
			openFileDialog.Filter = "Character File|*.json";
			openFileDialog.InitialDirectory = basePath;
			if ( openFileDialog.ShowDialog() == true )
			{
				try
				{
					//make sure it's a character and not a mission
					var file = File.ReadAllText( openFileDialog.FileName );
					if ( !file.Contains( "customCharacterGUID" ) || file.Contains( "missionGUID" ) )
						return null;

					string json = "";
					using ( StreamReader sr = new( openFileDialog.FileName ) )
					{
						json = sr.ReadToEnd();
					}
					var m = JsonConvert.DeserializeObject<CustomToon>( json );
					if ( m != null )
						return m;
					else
						return null;
				}
				catch ( Exception e )
				{
					MessageBox.Show( "Could not Import the Character.\r\n\r\nException:\r\n" + e.Message, "App Exception", MessageBoxButton.OK, MessageBoxImage.Error );
					return null;
				}
			}
			else
				return null;
		}

		public static CampaignPackage LoadCampaignPackage( string fullFilename )
		{
			CampaignPackage package = null;
			BitmapImage bitmap = null;
			byte[] iconBytesBuffer = new byte[0];
			string expectedVersion = @"""packageVersion"": ""2""";

			try
			{
				List<Mission> missionList = new();
				Dictionary<string, TranslatedMission> missionTranslationList = new();
				Dictionary<string, string> campaignInstList = new();
				List<string> warnings = new();

				//create the zip file
				using ( FileStream zipPath = new FileStream( fullFilename, FileMode.Open ) )
				{
					//open the archive
					using ( ZipArchive archive = new ZipArchive( zipPath, ZipArchiveMode.Read ) )
					{
						foreach ( var entry in archive.Entries )
						{
							//deserialize the CampaignPackage
							if ( entry.Name == "campaign_package.json" )
							{
								//open the package meta file
								using ( TextReader tr = new StreamReader( entry.Open() ) )
								{
									string s = tr.ReadToEnd();
									package = JsonConvert.DeserializeObject<CampaignPackage>( s );
									if ( !s.Contains( expectedVersion ) )
										throw new Exception( $"This Package isn't in the Version [2] format." );
								}
							}
							//deserialize the individual missions
							else if ( entry.Name.EndsWith( ".json" ) && entry.FullName.Contains( "Missions/" ) )
							{
								using ( TextReader tr = new StreamReader( entry.Open() ) )
								{
									missionList.Add( JsonConvert.DeserializeObject<Mission>( tr.ReadToEnd() ) );
								}
							}
							//translated campaign instructions
							else if ( entry.FullName.EndsWith( ".txt" ) && entry.FullName.Contains( "Translations/" ) )
							{
								using ( TextReader tr = new StreamReader( entry.Open() ) )
								{
									if ( !campaignInstList.ContainsKey( entry.Name ) )
										campaignInstList.Add( entry.Name, tr.ReadToEnd() );
									else
										warnings.Add( $"Duplicate Instruction translation found: {entry.Name}" );
								}
							}
							//translated missions
							else if ( entry.Name.EndsWith( ".json" ) && entry.FullName.Contains( "Translations/" ) )
							{
								using ( TextReader tr = new StreamReader( entry.Open() ) )
								{
									if ( !missionTranslationList.ContainsKey( entry.Name ) )
										missionTranslationList.Add( entry.Name, JsonConvert.DeserializeObject<TranslatedMission>( tr.ReadToEnd() ) );
									else
										warnings.Add( $"Duplicate Mission translation found: {entry.Name}" );
								}
							}
							else if ( (entry.Name.EndsWith( ".png" )) )//icon image
							{
								using ( var stream = new MemoryStream() )
								{
									using ( var s = entry.Open() )
									{
										s.CopyTo( stream );
										bitmap = new BitmapImage();
										bitmap.BeginInit();
										bitmap.StreamSource = stream;
										bitmap.CacheOption = BitmapCacheOption.OnLoad;
										bitmap.EndInit();

										//get bytes
										stream.Position = 0;
										iconBytesBuffer = new byte[stream.Length];
										stream.Read( iconBytesBuffer, 0, iconBytesBuffer.Length );
									}
								}
							}
						}

						//set the visible icon
						if ( bitmap != null )
						{
							package.bmpImage = bitmap;
							package.iconBytesBuffer = iconBytesBuffer;
						}
						else
						{
							package.SetDefaultIcon();
						}

						//now add all the missions and translations to the CampaignPackage
						foreach ( var item in package.campaignMissionItems )
						{
							//missions
							var m = missionList.Where( x => x.missionGUID == item.missionGUID ).FirstOr( null );
							//find the structure object that uses this Mission, if any
							if ( m != null )
							{
								var structure = package.campaignStructure.Where( x => x.missionID == m.missionGUID.ToString() ).FirstOr( null );
								item.mission = m;
								if ( structure != null )//set the Mission object back into structure
									structure.mission = m;
							}
							else
								throw new( $"Missing Mission in the zip archive:\n{item.missionName}\n{item.missionGUID}" );
						}
						foreach ( var item in package.campaignTranslationItems )
						{
							//translated missions and instructions
							if ( item.isInstruction )
								item.campaignInstructionTranslation = campaignInstList[item.fileName];
							else
								item.translatedMission = missionTranslationList[item.fileName];
						}
					}
				}

				if ( warnings.Count > 0 )
					MessageBox.Show( $"Problems were found in the loaded Campaign and will need to be manually fixed:\n\n{warnings.Aggregate( ( acc, cur ) => acc + cur + "\n" )}", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning );

				return package;
			}
			catch ( Exception ee )
			{
				MessageBox.Show( $"LoadCampaignPackage()::Error loading the Campaign Package.\n\n{ee.Message}", "App Exception", MessageBoxButton.OK, MessageBoxImage.Error );
				return null;
			}
		}

		public static bool SaveCampaignPackage( string fullPath, CampaignPackage package )
		{
			try
			{
				FileInfo fileInfo = new FileInfo( fullPath );

				//create the zip file
				using ( FileStream zipPath = new FileStream( fullPath, FileMode.Create ) )
				{
					//create the archive
					using ( ZipArchive archive = new ZipArchive( zipPath, ZipArchiveMode.Create ) )
					{
						//add the CampaignPackage object from memory
						var entry = archive.CreateEntry( "campaign_package.json" );
						using ( var packageStream = entry.Open() )
						{
							//add the CampaignPackage object from memory instead of from an actual file
							using ( var ms = new MemoryStream() )
							{
								//create campaign_package.json in memory
								using ( TextWriter tw = new StreamWriter( ms ) )
								{
									tw.Write( JsonConvert.SerializeObject( package, Formatting.Indented ) );
									tw.Flush();
									ms.Position = 0;
									//copy the memory stream to the archive
									ms.CopyTo( packageStream );
								}
							}
						}

						//add each mission
						var folderEntryMission = archive.CreateEntry( "Missions/" );//empty folder
						foreach ( var item in package.campaignMissionItems )
						{
							var missionEntry = archive.CreateEntry( $"Missions/{item.missionGUID}.json" );
							using ( var missionStream = missionEntry.Open() )
							{
								using ( var mmStream = new MemoryStream() )
								{
									//create the mission .json in memory
									using ( TextWriter tw = new StreamWriter( mmStream ) )
									{
										tw.Write( JsonConvert.SerializeObject( item.mission, Formatting.Indented ) );
										tw.Flush();
										mmStream.Position = 0;
										//copy the memory stream to the archive
										mmStream.CopyTo( missionStream );
									}
								}
							}
						}

						//add the Mission translations
						var folderEntryTranslation = archive.CreateEntry( "Translations/" );//empty folder
						foreach ( var item in package.campaignTranslationItems )
						{
							var translationEntry = archive.CreateEntry( $"Translations/{item.fileName}" );
							using ( var translationStream = translationEntry.Open() )
							{
								using ( var mmStream = new MemoryStream() )
								{
									//create the translation .json in memory
									using ( TextWriter tw = new StreamWriter( mmStream ) )
									{
										if ( !item.isInstruction )
											tw.Write( JsonConvert.SerializeObject( item.translatedMission, Formatting.Indented ) );
										else
											tw.Write( item.campaignInstructionTranslation );
										tw.Flush();
										mmStream.Position = 0;
										//copy the memory stream to the archive
										mmStream.CopyTo( translationStream );
									}
								}
							}
						}

						//add the icon
						var iconEntry = archive.CreateEntry( package.campaignIconName );
						using ( var bmpStream = iconEntry.Open() )
						{
							using ( var mmStream = new MemoryStream() )
							{
								mmStream.Write( package.iconBytesBuffer );
								mmStream.Flush();
								mmStream.Position = 0;
								//copy the memory stream to the archive
								mmStream.CopyTo( bmpStream );
							}
						}
					}
				}

				return true;
			}
			catch ( Exception ee )
			{
				MessageBox.Show( $"SaveCustomCampaign()::Could not export the Campaign Package.\n\n{ee.Message}", "App Exception", MessageBoxButton.OK, MessageBoxImage.Error );
			}

			return false;
		}

		public static T LoadJSON<T>( string fullPathToFilename ) where T : class
		{
			try
			{
				string json = "";
				using ( StreamReader sr = new( fullPathToFilename ) )
				{
					json = sr.ReadToEnd();
				}
				return JsonConvert.DeserializeObject<T>( json );
			}
			catch ( JsonReaderException e )
			{
				MessageBox.Show( "LoadJSON()::Error parsing JSON file.\n\nException:\n" + e.Message, "App Exception", MessageBoxButton.OK, MessageBoxImage.Error );
				return null;
			}
		}
	}
}
