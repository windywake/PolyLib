﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading.Tasks.Dataflow;
using Gridsum.DataflowEx;
using PolyLib.Animation;
using PolyLib.Threading;
using SkiaSharp;
using Exception = System.Exception;
using Math = System.Math;

namespace PolyLib.Animation
{
    internal class AnimationFlow : Dataflow<AnimationBase>
    {
        private readonly CurrentAnimationsBlock _animations;
        private readonly TransformBlock<AnimationBase[], RenderedFrame> _renderFrame;
        private readonly FrameQueueBlock<AnimationBase[]> _frameQueue;
        private RandomAnimationBlock _randomAnim;
        private readonly ActionBlock<RenderedFrame> _signalFrameRendered;

        public AnimationFlow(Action<RenderedFrame> notifyFrameReady, TaskScheduler uiScheduler) : base(DataflowOptions.Default)
        {

            _animations = new CurrentAnimationsBlock();
            _frameQueue = new FrameQueueBlock<AnimationBase[]>(new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount });

            _renderFrame = new TransformBlock<AnimationBase[], RenderedFrame>((arg) =>
            {
                var animFrame = new List<HashSet<AnimatedPoint>>();
                foreach (var anim in arg)
                {
                    if (!anim.IsSetup)
                    {
                        anim.SetupAnimation();
                    }

                    HashSet<AnimatedPoint> x = new HashSet<AnimatedPoint>();
                    //dont render a frame that won't be drawn
                    if (anim.CurrentFrame <= anim.NumFrames)
                        x = anim.RenderFrame(anim.CurrentFrame);

                    animFrame.Add(x);
                }

                //allows any derived draw function
                //newest animation determines how all current animations will be drawn
                var rend = new RenderedFrame(arg[arg.Length - 1].DrawPointFrame);

                //no use in "combining" animations unless there is more than 1 anim for this frame
                if (animFrame.Count > 1)
                {
                    var dict = new Dictionary<SKPoint, AnimatedPoint>();
                    //for each animation render for this frame
                    foreach (var frame in animFrame)
                    {
                        //for each point changed in the rendered animation
                        foreach (var pointChange in frame)
                        {
                            //if point has been previously animated, update it
                            if (dict.ContainsKey(pointChange.Point))
                            {
                                dict[pointChange.Point].XDisplacement += pointChange.XDisplacement;
                                dict[pointChange.Point].YDisplacement += pointChange.YDisplacement;
                            }
                            //or add it
                            else
                            {
                                dict[pointChange.Point] = pointChange;
                            }
                        }

                    }
                    //check each point after its duplicates have been accumulated
                    foreach (var point in dict.Values)
                    {
                        var p = dict[point.Point];

                        //only want to limit to max displacement if it has been specified
                        if (p.LimitDisplacement)
                        {
                            if (Math.Abs(p.XDisplacement) > p.MaxXDisplacement)
                            {
                                var oldDisp = p.XDisplacement;
                                p.XDisplacement = p.MaxXDisplacement;
                                if (oldDisp < 0)
                                    p.XDisplacement *= -1;
                            }

                            if (Math.Abs(p.YDisplacement) > p.MaxYDisplacement)
                            {
                                var oldDisp = p.YDisplacement;
                                p.YDisplacement = p.MaxYDisplacement;
                                if (oldDisp < 0)
                                    p.YDisplacement *= -1;
                            }
                        }
                    }

                    rend.FramePoints = dict.Values.ToList();
                }
                else
                {
                    rend.FramePoints = animFrame[0].ToList();
                }
                _animations.FrameRendered();

                return rend;
            }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount });

            //limit parallelism so that only one redraw update can occur
            _signalFrameRendered = new ActionBlock<RenderedFrame>(notifyFrameReady, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1, TaskScheduler = uiScheduler });

            _animations.LinkTo(_frameQueue, new DataflowLinkOptions {PropagateCompletion = true});
            _frameQueue.LinkTo(_renderFrame, new DataflowLinkOptions { PropagateCompletion = true });
            _renderFrame.LinkTo(_signalFrameRendered, new DataflowLinkOptions { PropagateCompletion = true });

            RegisterChild(_animations);
            RegisterChild(_frameQueue);
            RegisterChild(_renderFrame);
            RegisterChild(_signalFrameRendered);
        }

        public void StartRandomAnimationsLoop(int msBetweenRandomAnim)
        {
            if (_randomAnim != null)
            {
                //only want to start one random anim block at a time
                return;
            }
            _randomAnim = new RandomAnimationBlock(msBetweenRandomAnim);
            _randomAnim.LinkTo(_animations, new DataflowLinkOptions { PropagateCompletion = true });
            RegisterChild(_randomAnim);
        }

        public void StopRandomAnimationsLoop()
        {
            //force completion of the flow, which will make the flow get recreated
            //engine determines if a new animation should be added
            _randomAnim.Complete();
        }

        internal void UpdateFPS(int fps)
        {
            if (_randomAnim != null)
                _frameQueue.UpdateFPS(fps);
        }

        internal void UpdateRandomAnimTriangulations(List<Triangulation> triangulations)
        {
            if (_randomAnim != null)
                _randomAnim.UpdateSourceTriangulations(triangulations);
            
        }

        internal void SetAnimCreatorsForRandomLoop(List<Func<Triangulation, AnimationBase>> animCreators)
        {
            if (_randomAnim != null)
                _randomAnim.SetAnimationCreators(animCreators);
        }

        public override ITargetBlock<AnimationBase> InputBlock
        {
            get { return _animations; }
        }
    }
}
