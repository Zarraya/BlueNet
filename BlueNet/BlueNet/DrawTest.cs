
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Graphics;
using Android.Graphics.Drawables;

namespace BlueNet
{
			
	public class DrawTest : View
	{

		private Path drawPath;
		private Paint drawPaint, canvasPaint;
		private Canvas drawCanvas;
		private Bitmap canvasBitmap;

		private int w,h;

		public DrawTest(Context context) : base(context){



			drawPath = new Path ();
			drawPaint = new Paint ();

			drawPaint.AntiAlias = true;
			drawPaint.StrokeWidth = 15;
			drawPaint.SetStyle (Paint.Style.Stroke);
			drawPaint.StrokeJoin = Paint.Join.Round;
			drawPaint.StrokeCap = Paint.Cap.Round;

			canvasPaint = new Paint (PaintFlags.Dither);
		}

		protected override void OnDraw (Android.Graphics.Canvas canvas)
		{
			canvas.DrawBitmap (canvasBitmap, 0, 0, canvasPaint);
			canvas.DrawPath (drawPath, drawPaint);
		}

		public override bool OnTouchEvent (MotionEvent event1)
		{
			Console.WriteLine ("touch event");

			float x = event1.GetX ();
			float y = event1.GetY ();

			switch (event1.Action) {

			case MotionEventActions.Down:
				drawPath.MoveTo (x, y);
				break;
			case MotionEventActions.Move:
				drawPath.LineTo (x, y);
				break;
			case MotionEventActions.Up:
				drawCanvas.DrawPath (drawPath, drawPaint);
				drawPath.Reset ();
				break;
			default:
				return false;
			}

			Invalidate ();
			return true;
		}

		public void setColor(bool erase){

			Invalidate ();

			if (!erase) {

				drawPaint.Color = Color.Black;
			} else {
				drawPaint.Color = Color.White;
			}
		}

		protected override void OnSizeChanged (int w, int h, int oldw, int oldh)
		{
			this.w = w;
			this.h = h;
			base.OnSizeChanged (w, h, oldw, oldh);
			canvasBitmap = Bitmap.CreateBitmap (w, h, Bitmap.Config.Argb8888);
			drawCanvas = new Canvas (canvasBitmap);
		}

		public void clear(){

			Paint temp = new Paint ();
			temp.SetStyle (Paint.Style.Fill);
			temp.Color = Color.White;

			Invalidate ();

			drawCanvas.DrawRect (new Rect (0, 0, w, h), temp);
		}

		public Bitmap done(){
		
			return canvasBitmap;
		}
	}
}

