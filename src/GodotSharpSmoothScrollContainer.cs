using Godot;

/// <summary>
/// Container with smooth scroll effect.
/// </summary>
[GlobalClass]
public partial class SmoothScrollContainer : ScrollContainer
{
    /// <summary>
    /// Used to test, this will remove all children nodes and add test nodes.
    /// You can use it to test smooth scroll effect.
    /// </summary>
    [Export]
    private bool _enableTest = false;

    /// <summary>
    /// Scroll speed.
    /// </summary>
    [Export(PropertyHint.Range, "0,1000,1,suffix:px")]
    public float Speed = 100;

    /// <summary>
    /// Animation time for scrolling.
    /// </summary>
    [Export(PropertyHint.Range, "0,1,0.05,suffix:sec")]
    public float AniTime = 0.4f;

    /// <summary>
    /// Enable drag using mouse, touch or pen.<br/>
    /// * It's private, you should determine whether to use it before creating an instance.<br/>
    /// * MouseFilter of all child nodes should be Pass.
    /// You can call RecursiveSetMouseFilterPass(parentNode) to set MouseFilter.
    /// </summary>
    [Export]
    private bool _enableDrag = true;

    /// <summary>
    /// Enable hide scrollbar over time.<br/>
    /// * It's private, you should determine whether to use it before creating an instance.
    /// </summary>
    [Export]
    private bool _enableAutoHideBar = true;

    /// <summary>
    /// Time it takes for the scrollbar to hide.
    /// </summary>
    [Export(PropertyHint.Range, "0,30,0.1,suffix:sec")]
    public float AutoHideBarTime = 3;

    /// <summary>
    /// Enable scroll follow focus.<br/>
    /// * It's private, you should determine whether to use it before creating an instance.
    /// </summary>
    [Export]
    private bool _enableFollowFocus = true;

    /// <summary>
    /// Set horizontal scrollbar target value.
    /// </summary>
    public float HBarTargetValue
    {
        set
        {
            var lastHBarTargetValue = _hBarTargetValue;
            _hBarTargetValue = Mathf.Clamp(value, 0, (float)(_hBar.MaxValue - _hBar.Page));
            if (_hBarTargetValue == lastHBarTargetValue)
            {
                return;
            }
            SmoothScroll(_hBar, ref _hBarTween, _hBarTargetValue);
        }
        get { return _hBarTargetValue; }
    }
    private float _hBarTargetValue = 0;

    /// <summary>
    /// Set vertical scrollbar target value.
    /// </summary>
    public float VBarTargetValue
    {
        set
        {
            var lastVBarTargetValue = _vBarTargetValue;
            _vBarTargetValue = Mathf.Clamp(value, 0, (float)(_vBar.MaxValue - _vBar.Page));
            if (_vBarTargetValue == lastVBarTargetValue)
            {
                return;
            }
            SmoothScroll(_vBar, ref _vBarTween, _vBarTargetValue);
        }
        get { return _vBarTargetValue; }
    }
    private float _vBarTargetValue = 0;

    /// <summary>
    /// Content node of container.<br/>
    /// * If you change content node, directly set this or call UpdateContentNode().
    /// </summary>
    public Control ContentNode;
    private HScrollBar _hBar;
    private VScrollBar _vBar;
    private Tween _hBarTween;
    private Tween _vBarTween;
    private Tween _barHideTween;
    private Timer _barHideTimer = new();
    private bool _isDragging = false;

    /// <summary>
    /// Default constructor, do nothing.
    /// * Don't remove it, because engine will use it to create instance.
    /// </summary>
    public SmoothScrollContainer() { }

    /// <summary>
    /// Constructor, change some private instance fields.
    /// </summary>
    /// <param name="enableDrag">Enable drag using mouse, touch or pen.</param>
    /// <param name="enableHideBarOverTime">Enable hide scrollbar over time.</param>
    /// <param name="enableFollowFocus">Enable scroll follow focus.</param>
    public SmoothScrollContainer(
        bool enableDrag,
        bool enableHideBarOverTime,
        bool enableFollowFocus
    )
    {
        _enableDrag = enableDrag;
        _enableAutoHideBar = enableHideBarOverTime;
        _enableFollowFocus = enableFollowFocus;
    }

    public override void _Ready()
    {
        // Test mode to populate the container with test nodes.
        if (_enableTest && !Engine.IsEditorHint())
        {
            foreach (var node in GetChildren())
            {
                RemoveChild(node);
                node.QueueFree();
            }
            GridContainer gridContainer = new() { Columns = 10 };
            AddChild(gridContainer);
            for (int i = 0; i < 100; i++)
            {
                Button button =
                    new() { CustomMinimumSize = new(300, 300), Text = $"Test\nButton{i + 1}" };
                gridContainer.AddChild(button);
            }
        }

        UpdateContentNode();
        if (_enableDrag)
        {
            RecursiveSetMouseFilterPass(this);
        }

        _hBar = GetHScrollBar();
        _vBar = GetVScrollBar();
        _hBar.GuiInput += GuiInputMouseWheel;
        _vBar.GuiInput += GuiInputMouseWheel;

        _hBarTween = CreateTween();
        _hBarTween.Pause();
        _vBarTween = CreateTween();
        _vBarTween.Pause();
        _barHideTween = CreateTween();
        _barHideTween.Pause();

        AddChild(_barHideTimer, false, InternalMode.Back);

        if (_enableAutoHideBar)
        {
            _hBar.Modulate = Colors.Transparent;
            _vBar.Modulate = Colors.Transparent;
            _barHideTimer.Timeout += HideScrollBar;
        }

        FollowFocus = false;
        if (_enableFollowFocus)
        {
            GetViewport().GuiFocusChanged += OnGuiFocusChanged;
        }
    }

    private void GuiInputMouseWheel(InputEvent @event)
    {
        if (@event is InputEventMouseButton eventMouseButton && eventMouseButton.Pressed)
        {
            var factor = (eventMouseButton.Factor == 0) ? 1 : eventMouseButton.Factor;
            switch (eventMouseButton.ButtonIndex)
            {
                case MouseButton.WheelUp:
                    if (eventMouseButton.ShiftPressed)
                    {
                        HBarTargetValue -= Speed * factor;
                    }
                    else
                    {
                        if (_vBar.MaxValue == _vBar.Page)
                        {
                            HBarTargetValue -= Speed * factor;
                        }
                        else
                        {
                            VBarTargetValue -= Speed * factor;
                        }
                    }
                    GetViewport().SetInputAsHandled();
                    break;
                case MouseButton.WheelDown:
                    if (eventMouseButton.ShiftPressed)
                    {
                        HBarTargetValue += Speed * factor;
                    }
                    else
                    {
                        if (_vBar.MaxValue == _vBar.Page)
                        {
                            HBarTargetValue += Speed * factor;
                        }
                        else
                        {
                            VBarTargetValue += Speed * factor;
                        }
                    }
                    GetViewport().SetInputAsHandled();
                    break;
                case MouseButton.WheelLeft:
                    HBarTargetValue -= Speed * factor;
                    GetViewport().SetInputAsHandled();
                    break;
                case MouseButton.WheelRight:
                    HBarTargetValue += Speed * factor;
                    GetViewport().SetInputAsHandled();
                    break;
            }
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        GuiInputMouseWheel(@event);
        if (_enableDrag)
        {
            if (@event is InputEventMouseButton eventMouseButton)
            {
                if (eventMouseButton.ButtonIndex == MouseButton.Left)
                {
                    if (eventMouseButton.Pressed)
                    {
                        _isDragging = true;
                    }
                    else
                    {
                        _isDragging = false;
                    }
                    GetViewport().SetInputAsHandled();
                }
            }
            else if (@event is InputEventScreenTouch eventScreenTouch)
            {
                if (eventScreenTouch.Pressed)
                {
                    _isDragging = true;
                }
                else
                {
                    _isDragging = false;
                }
                GetViewport().SetInputAsHandled();
            }
            else if (@event is InputEventMouseMotion eventMouseMotion)
            {
                if (_isDragging)
                {
                    HBarTargetValue -= eventMouseMotion.Relative.X;
                    VBarTargetValue -= eventMouseMotion.Relative.Y;
                    GetViewport().SetInputAsHandled();
                }
            }
            else if (@event is InputEventScreenDrag eventScreenDrag)
            {
                if (_isDragging)
                {
                    HBarTargetValue -= eventScreenDrag.Relative.X;
                    VBarTargetValue -= eventScreenDrag.Relative.Y;
                    GetViewport().SetInputAsHandled();
                }
            }
            else if (@event is InputEventPanGesture eventPanGesture)
            {
                HBarTargetValue -= eventPanGesture.Delta.X;
                VBarTargetValue -= eventPanGesture.Delta.Y;
                GetViewport().SetInputAsHandled();
            }
        }
    }

    private void SmoothScroll(ScrollBar bar, ref Tween tween, float target)
    {
        if (_enableAutoHideBar)
        {
            ShowScrollBar();
        }
        tween.Kill();
        tween = CreateTween();
        tween.TweenProperty(bar, "value", target, _isDragging ? 0.1 : AniTime);
    }

    private void ShowScrollBar()
    {
        _barHideTween.Pause();
        _barHideTimer.Start(AutoHideBarTime);
        _hBar.Modulate = Colors.White;
        _vBar.Modulate = Colors.White;
    }

    private void HideScrollBar()
    {
        _barHideTween.Kill();
        _barHideTween = CreateTween().SetParallel(true);
        _barHideTween.TweenProperty(_hBar, "modulate", Colors.Transparent, 0.4);
        _barHideTween.TweenProperty(_vBar, "modulate", Colors.Transparent, 0.4);
    }

    private void OnGuiFocusChanged(Control control)
    {
        if (ContentNode.IsAncestorOf(control))
        {
            var pageRect = new Rect2(
                _hBarTargetValue,
                _vBarTargetValue,
                (float)_hBar.Page,
                (float)_vBar.Page
            );
            var controlRect = control.GetRect();
            float hBarMove = 0;
            float vBarMove = 0;
            if (pageRect.Position.X > controlRect.Position.X)
            {
                hBarMove = controlRect.Position.X - pageRect.Position.X;
            }
            else if (controlRect.End.X > pageRect.End.X)
            {
                hBarMove = controlRect.End.X - pageRect.End.X;
            }

            if (pageRect.Position.Y > controlRect.Position.Y)
            {
                vBarMove = controlRect.Position.Y - pageRect.Position.Y;
            }
            else if (controlRect.End.Y > pageRect.End.Y)
            {
                vBarMove = controlRect.End.Y - pageRect.End.Y;
            }
            HBarTargetValue += hBarMove;
            VBarTargetValue += vBarMove;
        }
    }

    /// <summary>
    /// Method to update the content node.
    /// </summary>
    public void UpdateContentNode()
    {
        foreach (var node in GetChildren())
        {
            if (node is Control control && control is not ScrollBar)
            {
                ContentNode = control;
                break;
            }
        }
    }

    /// <summary>
    /// Static method to recursively set MouseFilter to Pass for child nodes.<br/>
    /// </summary>
    /// <param name="parentNode">Parent node.</param>
    public static void RecursiveSetMouseFilterPass(Node parentNode)
    {
        foreach (var childNode in parentNode.GetChildren())
        {
            if (childNode is Control control)
            {
                control.MouseFilter = MouseFilterEnum.Pass;
                if (control.GetChildCount() != 0)
                {
                    RecursiveSetMouseFilterPass(childNode);
                }
            }
        }
    }
}
