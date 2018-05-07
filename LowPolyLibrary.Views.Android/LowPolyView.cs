﻿using System;
using System.Collections.Generic;
using System.Text;
using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using LowPolyLibrary.Animation;
using SkiaSharp;

namespace LowPolyLibrary.Views.Android
{
    public class LowPolyView : FrameLayout
    {
        public TriangulationView TriangulationView { get; private set; }
        public AnimationUpdateView AnimationUpdateView { get; private set; }

        public Triangulation CurrentTriangulation => TriangulationView.Triangulation;

#region Constructors

        protected LowPolyView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
            Init();
        }

        public LowPolyView(Context context) : base(context)
        {
            Init();
        }

        public LowPolyView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            Init();
        }

        public LowPolyView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            Init();
        }

#endregion

        private void Init()
        {
            TriangulationView = new TriangulationView(Context);
            AnimationUpdateView = new AnimationUpdateView(Context);

            var layoutParams = new FrameLayout.LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent);
            TriangulationView.LayoutParameters = layoutParams;
            AnimationUpdateView.LayoutParameters = layoutParams;

            AddView(TriangulationView);
            AddView(AnimationUpdateView);
        }

        public LowPolyView ResizeView(int boundsWidth, int boundsHeight, View.IOnTouchListener listener = null)
        {
            //SKCanvasView cannot change size. Instead, generate a new one in this views place

            var parent = ((ViewGroup)Parent);
            var index = parent.IndexOfChild(this);
            parent.RemoveView(this);
            var newCanvasView = new LowPolyView(Context);

            //setup listeners
            newCanvasView.SetOnTouchListener(listener);
            SetOnTouchListener(null);

            parent.AddView(newCanvasView, index, new FrameLayout.LayoutParams(boundsWidth, boundsHeight));
            return newCanvasView;
        }

        public void UpdateTriangulation(Triangulation tri){
            TriangulationView.UpdateTriangulation(tri);
            Invalidate();
        }

        public void AddAnimation(AnimationBase anim) => AnimationUpdateView.AddAnimation(anim);

        //protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
        //{
        //    base.OnSizeChanged(w, h, oldw, oldh);
        //    ViewTreeObserver.AddOnGlobalLayoutListener(new GlobalLayoutListener((obj) =>
        //    {
        //        ViewTreeObserver.RemoveOnGlobalLayoutListener(obj);
        //        Triangulation = new LowPolyLibrary.Triangulation(Width, Height, Variance, CellSize);
        //        Invalidate();
        //    }));
        //}
    }
}
