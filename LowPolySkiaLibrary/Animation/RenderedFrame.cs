﻿using System;
using System.Collections.Generic;
using LowPolyLibrary.BitmapPool;

namespace LowPolyLibrary.Animation
{
    //class used to store data necessary to draw a rendered frame
    public class RenderedFrame
    {
        public List<AnimatedPoint> FramePoints = null;

        public Func<List<AnimatedPoint>, IManagedBitmap> DrawFunction = null;

        public RenderedFrame(Func<List<AnimatedPoint>, IManagedBitmap> funct)
        {
            DrawFunction = funct;
        }

        public RenderedFrame(Func<List<AnimatedPoint>, IManagedBitmap> funct, List<AnimatedPoint> points )
        {
            FramePoints = points;
            DrawFunction = funct;
        }
    }
}