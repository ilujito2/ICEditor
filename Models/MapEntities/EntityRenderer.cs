﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Imperial_Commander_Editor
{
  public class EntityRenderer
  {
    public Point dims;

    public Shape entityShape { get; set; }
    public Image entityImage { get; set; }

    public SolidColorBrush selectedBGColor = new( Colors.Transparent );
    public SolidColorBrush unselectedBGColor = new( Colors.Transparent );
    public SolidColorBrush selectedStrokeColor = new( Colors.White );
    public SolidColorBrush unselectedStrokeColor = new( Colors.Black );
    public SolidColorBrush highlightColor = new( Colors.Yellow );
    public double selectedStrokeWidth, unselectedStrokeWidth, highlightStrokeWidth;

    public IMapEntity mapEntity;
    public Vector where;
    public bool showPanel;
    public double scale;
    public int selectedZ;

    public EntityRenderer( IMapEntity entity, Vector where, bool showPanel, double scale, Point dimensions )
    {
      dims = dimensions;
      this.where = where;
      this.showPanel = showPanel;
      this.scale = scale;
      mapEntity = entity;
      this.mapEntity.entityPosition = where;
      selectedZ = 100;
      selectedStrokeWidth = 1;
      unselectedStrokeWidth = 1;
      highlightStrokeWidth = 1;
    }

    public void BuildShape( TokenShape tokenShape )
    {
      var ww = Utils.mainWindow.Width;
      var hh = Utils.mainWindow.Height;
      var tx = Math.Abs( Math.Min( 0, where.X ) ) + (ww / 2) - (showPanel ? 225 : 0);
      var ty = Math.Abs( Math.Min( 0, where.Y ) ) + (hh / 2) - 125;

      if ( tokenShape == TokenShape.Square )
      {
        entityShape = new Rectangle();
        entityShape.Width = 10 * dims.X;
        entityShape.Height = 10 * dims.Y;
        entityShape.Fill = new SolidColorBrush( Colors.SaddleBrown );
        Canvas.SetLeft( entityShape, tx / scale );
        Canvas.SetTop( entityShape, ty / scale );
      }
      else if ( tokenShape == TokenShape.Circle )
      {
        entityShape = new Ellipse();
        entityShape.Width = 10 * dims.X;
        entityShape.Height = 10 * dims.Y;
        entityShape.Fill = new SolidColorBrush( Colors.Gray );
        Canvas.SetLeft( entityShape, tx / scale );
        Canvas.SetTop( entityShape, ty / scale );
      }
      else if ( tokenShape == TokenShape.Rectangle )
      {
        //fill out the grid square with a clickable, transparent shape
        entityShape = new Rectangle();
        entityShape.Width = 10 * dims.X;
        entityShape.Height = 10 * dims.Y;
        entityShape.Fill = new SolidColorBrush( Colors.Transparent );
        Canvas.SetLeft( entityShape, tx / scale );
        Canvas.SetTop( entityShape, ty / scale );
      }

      entityShape.DataContext = mapEntity;
      RoundPosition();
    }

    public void BuildImage( string imageURI )
    {
      var ww = Utils.mainWindow.Width;
      var hh = Utils.mainWindow.Height;
      var tx = Math.Abs( Math.Min( 0, where.X ) ) + (ww / 2) - (showPanel ? 225 : 0);
      var ty = Math.Abs( Math.Min( 0, where.Y ) ) + (hh / 2) - 125;

      ImageSource image = new BitmapImage( new Uri( imageURI ) );
      entityImage = new();
      entityImage.Width = 10 * dims.X;
      entityImage.Height = 10 * dims.Y;
      entityImage.Source = image;
      entityImage.IsHitTestVisible = false;
      Canvas.SetLeft( entityImage, tx / scale );
      Canvas.SetTop( entityImage, ty / scale );

      RoundPosition();
    }

    public void RemoveVisual()
    {
      if ( entityShape != null )
      {
        Canvas c = entityShape.Parent as Canvas;
        c.Children.Remove( entityShape );
      }
      if ( entityImage != null )
      {
        Canvas c = entityImage.Parent as Canvas;
        c.Children.Remove( entityImage );
      }
    }
    public void Update( Point delta )
    {
      var mx = Canvas.GetLeft( entityShape ) + delta.X;
      var my = Canvas.GetTop( entityShape ) + delta.Y;
      Canvas.SetLeft( entityShape, mx );
      Canvas.SetTop( entityShape, my );

      if ( entityImage != null )
      {
        Canvas.SetLeft( entityImage, mx );
        Canvas.SetTop( entityImage, my );
      }

      where = GetPosition();
      mapEntity.entityPosition = where;
    }
    public void Rotate( int dir )
    {
      var t = entityShape.RenderTransform as RotateTransform ?? new RotateTransform();
      t.Angle += 90 * dir;
      entityShape.RenderTransform = new RotateTransform( t.Angle );
      if ( entityImage != null )
        entityImage.RenderTransform = new RotateTransform( t.Angle );
      mapEntity.entityRotation = t.Angle;
    }
    public void RoundPosition()
    {
      var mx = Canvas.GetLeft( entityShape ).RoundOff( 10 );
      var my = Canvas.GetTop( entityShape ).RoundOff( 10 );
      Canvas.SetLeft( entityShape, mx );
      Canvas.SetTop( entityShape, my );

      if ( entityImage != null )
      {
        Canvas.SetLeft( entityImage, mx );
        Canvas.SetTop( entityImage, my );
      }

      where = GetPosition();
      mapEntity.entityPosition = where;
    }
    public void Select()
    {
      entityShape.Stroke = selectedStrokeColor;
      entityShape.StrokeThickness = selectedStrokeWidth;
      entityShape.Fill = selectedBGColor;
      Panel.SetZIndex( entityShape, selectedZ );
      if ( entityImage != null )
        Panel.SetZIndex( entityImage, selectedZ - 1 );
    }
    public void Unselect()
    {
      entityShape.Stroke = unselectedStrokeColor;
      entityShape.StrokeThickness = unselectedStrokeWidth;
      entityShape.Fill = unselectedBGColor;
      Panel.SetZIndex( entityShape, selectedZ - 10 );
      if ( entityImage != null )
        Panel.SetZIndex( entityImage, selectedZ - 10 - 1 );
    }
    public void Highlight()
    {
      entityShape.Stroke = highlightColor;
      entityShape.StrokeThickness = highlightStrokeWidth;
    }
    public void RemoveHighlight()
    {
      entityShape.StrokeThickness = 0;
    }
    public void SetPosition( Vector w )
    {
      where = w;
      mapEntity.entityPosition = where;
      Canvas.SetLeft( entityShape, where.X );
      Canvas.SetTop( entityShape, where.Y );

      if ( entityImage != null )
      {
        Canvas.SetLeft( entityImage, where.X );
        Canvas.SetTop( entityImage, where.Y );
      }
    }
    public Vector GetPosition()
    {
      var mx = Canvas.GetLeft( entityShape );
      var my = Canvas.GetTop( entityShape );
      return new( mx, my );
    }
    public void SetRotation( double r )
    {
      mapEntity.entityRotation = r;
      var t = entityShape.RenderTransform as RotateTransform ?? new RotateTransform();
      t.Angle = r;
      entityShape.RenderTransform = new RotateTransform( t.Angle );
      if ( entityImage != null )
        entityImage.RenderTransform = new RotateTransform( t.Angle );
    }
  }
}
