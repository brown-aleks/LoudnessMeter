using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Diagnostics;
using System.Threading;

namespace LoudnessMeterUI;

public partial class AnimatedPopup : ContentControl
{
    #region Private Members

    /// <summary>
    /// The underlay control for closing this popup
    /// </summary>
    private Control mUnderlayControl;

    /// <summary>
    /// Indicates if this is first time we are animated
    /// </summary>
    private bool mFirstAnimation = true;

    /// <summary>
    /// Indicates if we have captured the opacity value yet
    /// </summary>
    private bool mOpacityCaptured = false;

    /// <summary>
    /// Store the controls original Opacity value at startup
    /// </summary>
    private double mOriginalOpacity;
    
    // Get a 60 fps timespan
    private TimeSpan mFramerate = TimeSpan.FromSeconds(1 / 60.0);

    //  Calculate total ticks that make up the animation time
    private int mTotalTicks => (int)(_animationTime.TotalSeconds / mFramerate.TotalSeconds);

    /// <summary>
    /// Stored the control desired size
    /// </summary>
    private Size mDesiredSize;

    /// <summary>
    /// A flag for when we are animating
    /// </summary>
    private bool mAnimating;

    /// <summary>
    /// Keeps track of if we have found the desired 100% width/height auto size
    /// </summary>
    private bool mSizeFound;

    /// <summary>
    /// The animation timer
    /// </summary>
    private DispatcherTimer mAnimationTimer;

    /// <summary>
    /// The timeout timer to detect when auto-sizing has finished firing
    /// </summary>
    private Timer mSizingTimer;

    /// <summary>
    /// The current position in the animation
    /// </summary>
    private int mAnimationCurrentTick;


    #endregion

    #region PublicProperties

    /// <summary>
    /// Indicates if the control is currently opened
    /// </summary>
    public bool IsOpened => mAnimationCurrentTick >= mTotalTicks;

    #region Open

    /// <summary>
    /// Sets whether the control should be opening or closing
    /// </summary>
    private bool _open;

    public static DirectProperty<AnimatedPopup, bool> OpenProperty = AvaloniaProperty.RegisterDirect<AnimatedPopup, bool>(
        nameof(Open), o => o.Open, (o, v) => o.Open = v );

    /// <summary>
    /// Property to set whether the control should be open or closed
    /// </summary>
    public bool Open
    {
        get => _open;
        set
        {
            //  If we are opening...
            if (value)
            {
                //  If the value has not changed...
                if (value == _open)
                    //  Do nosing
                    return;

                //  If the parent is a grid...
                if (Parent is Grid grid)
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        //  Set grid row/column span
                        if (grid.RowDefinitions?.Count > 0)
                            mUnderlayControl.SetValue(Grid.RowSpanProperty, grid.RowDefinitions.Count);

                        if (grid.ColumnDefinitions?.Count > 0)
                            mUnderlayControl.SetValue(Grid.ColumnProperty, grid.ColumnDefinitions.Count);

                        //  Insert the underlay control
                        if(!grid.Children.Contains(mUnderlayControl))
                            grid.Children.Insert(0, mUnderlayControl);
                    });
                }
            }
            //  If closing...
            else
            {
                //  if the control the current ful open...
                if(IsOpened)
                    //  Update desired size
                    UpdateDesiredSize();
            }

            //  Update animation
            UpdateAnimation();

            //  Raise the property changed event
            SetAndRaise(OpenProperty, ref _open, value);
        }
    }

    #endregion

    #region Animation Time

    private TimeSpan _animationTime = TimeSpan.FromSeconds(3);

    public static readonly DirectProperty<AnimatedPopup, TimeSpan> AnimationTimeProperty = AvaloniaProperty.RegisterDirect<AnimatedPopup, TimeSpan>(
        nameof(AnimationTime), o => o.AnimationTime, (o, v) => o.AnimationTime = v);

    public TimeSpan AnimationTime
    {
        get => _animationTime;
        set => SetAndRaise(AnimationTimeProperty, ref _animationTime, value);
    }

    #endregion

    #region Animate opacity

    private bool _animateOpacity = true;

    public static readonly DirectProperty<AnimatedPopup, bool> AnimatedOpacityProperty = AvaloniaProperty.RegisterDirect<AnimatedPopup,bool>(
        nameof(AnimateOpacity), o => o.AnimateOpacity, (o,v) => o.AnimateOpacity = v);

    public bool AnimateOpacity
    {
        get => _animateOpacity;
        set => SetAndRaise(AnimatedOpacityProperty, ref _animateOpacity, value);
    }

    #endregion

    #region Underlay Opacity

    private double _underlayOpacity = 0.2;

    public static readonly DirectProperty<AnimatedPopup, double> UnderlayOpacityProperty = AvaloniaProperty.RegisterDirect<AnimatedPopup, double>(
        nameof(UnderlayOpacity), o => o.UnderlayOpacity, (o, v) => o.UnderlayOpacity = v);

    public double UnderlayOpacity
    {
        get => _underlayOpacity;
        set => SetAndRaise(UnderlayOpacityProperty, ref _underlayOpacity, value);
    }

    #endregion

    #endregion

    #region Public Commands

    [RelayCommand]
    public void BeginOpen()
    {
        Open = true;
    }

    [RelayCommand]
    public void BeginClose()
    {
        Open = false;
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Default constructor
    /// </summary>
    public AnimatedPopup()
    {
        //  Make a new underlay control
        mUnderlayControl = new Border()
        {
            Background = Brushes.Black,
            Opacity = 0,
            ZIndex = 9,
        };

        //  On press, close popup
        mUnderlayControl.PointerPressed += (sender, args) =>
        {
            BeginClose();
        };

        //  Make a new dispatch timer
        mAnimationTimer = new DispatcherTimer
        {
            //  Set the timer to run 60 times a second
            Interval = mFramerate
        };

        mSizingTimer = new Timer(t =>
        {
            //  if we have already calculated the size...
            if (mSizeFound)
                //  No longer accept new sizes
                return;

            //  We have now found our desired size
            mSizeFound = true;

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                //  Update the desired size
                UpdateDesiredSize();

                //  Update animation
                UpdateAnimation();
            });
        });

        //  Callback on every tick
        mAnimationTimer.Tick += (s,e) => AnimationTick();
    }

    #endregion

    #region Private Method

    /// <summary>
    /// Update the animation desired size based on the current visuals desired size
    /// </summary>
    private void UpdateDesiredSize() => mDesiredSize = DesiredSize - Margin;

    /// <summary>
    /// Calculate and start any new required animations
    /// </summary>
    private void UpdateAnimation()
    {
        //  Do nothing if we still haven't found our initial size
        if (!mSizeFound)
            return;

        //  Start the animation thread again
        mAnimationTimer.Start();
    }

    /// <summary>
    /// Should be called when an open or close transition has completed
    /// </summary>
    private void AnimationCompleted()
    {
        //  If open...
        if (_open)
        {
            //  Set size to desired size
            Width = double.NaN;
            Height = double.NaN;

            //  Make sure opacity is set to original value
            Opacity = mOriginalOpacity;
        }
        //  If closed...
        else
        {
            //  Set size to zero
            Width = 0;
            Height = 0;

            //  If the parent is a grid...
            if (Parent is Grid grid)
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    //  Reset Opacity
                    mUnderlayControl.Opacity = 0;

                    //  Remove underlay
                    if (grid.Children.Contains(mUnderlayControl))
                        grid.Children.Remove(mUnderlayControl);
                });
            }
        }
    }

    /// <summary>
    /// Update controls sizes based on the next tick of an animation
    /// </summary>
    private void AnimationTick()
    {
        //  If this is the first call after calculating the desired size...
        if(mFirstAnimation)
        {
            //  Clear the flag
            mFirstAnimation = false;

            //  Stop this animation timer
            mAnimationTimer.Stop();

            //  Reset opacity
            Opacity = mOriginalOpacity;

            //  Set the final size
            AnimationCompleted();

            //  Do on this tick
            return;
        }

        //  If we have reached the end of our animation...
        if ((_open && mAnimationCurrentTick >= mTotalTicks) ||
            (!_open && mAnimationCurrentTick == 0))
        {
            //  Stop this animation timer
            mAnimationTimer.Stop();

            //  Set the final size
            AnimationCompleted();

            //  Clear animating
            mAnimating = false;

            //  Break out of code
            return;
        }

        //  Set animating flag
        mAnimating = true;

        //  Move the tick in the right direction
        mAnimationCurrentTick += _open ? 1 : -1;

        //  Get percentage of the way trough the current animation
        var percentageAnimated = (float)mAnimationCurrentTick / mTotalTicks;

        //  Make an animation easing
        var easing = new QuadraticEaseIn();

        //  Calculate final width and height
        var finalWidth = mDesiredSize.Width * easing.Ease(percentageAnimated);
        var finalHeight = mDesiredSize.Height * easing.Ease(percentageAnimated);

        //  Do our animation
        Width = finalWidth;
        Height = finalHeight;

        //  Animate opacity
        if(AnimateOpacity)
            Opacity = mOriginalOpacity * easing.Ease(percentageAnimated);

        //  Animate underlay
        mUnderlayControl.Opacity = _underlayOpacity * easing.Ease(percentageAnimated);

        //Debug.WriteLine($"Current tick: {mAnimationCurrentTick}");
    }

    #endregion

    public override void Render(DrawingContext context)
    {
        //  If we have not yet found the desired size...
        if (!mSizeFound)
        {
            //  If we have not yet captured the opacity
            if (!mOpacityCaptured)
            {
                //  Set flag to true
                mOpacityCaptured = true;

                //  Remember original control opacity
                mOriginalOpacity = Opacity;

                //  Hide control
                //Opacity = 0;
                Dispatcher.UIThread.InvokeAsync(() => { Opacity = 0; });
            }

            mSizingTimer.Change(100, int.MaxValue);
        }

        base.Render(context);
    }
}