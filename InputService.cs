using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace CyclopsEngine;

public enum MouseButton
{
    Left,
    Right,
    Middle
}

public sealed class InputService
{
    private static readonly TimeSpan DoubleClickWindow = TimeSpan.FromMilliseconds(500);
    private const float ClickMovementTolerance = 10f;

    private readonly Game _game;
    private readonly bool[] _clickTrackers = new bool[3];
    private readonly Point[] _pressPositions = new Point[3];
    private readonly TimeSpan[] _lastClickTimes = new TimeSpan[3];
    private readonly Point[] _lastClickPositions = new Point[3];
    private readonly bool[] _hasPreviousClick = new bool[3];
    private TimeSpan _elapsed;

    public MouseState PreviousMouseState { get; private set; }

    public MouseState CurrentMouseState { get; private set; }

    public KeyboardState PreviousKeyboardState { get; private set; }

    public KeyboardState CurrentKeyboardState { get; private set; }

    public Point MousePosition => CurrentMouseState.Position;

    public Vector2 MouseVelocity => new(
        CurrentMouseState.X - PreviousMouseState.X,
        CurrentMouseState.Y - PreviousMouseState.Y);

    public bool LeftButtonPressed => CurrentMouseState.LeftButton == ButtonState.Pressed;

    public bool RightButtonPressed => CurrentMouseState.RightButton == ButtonState.Pressed;

    public bool MiddleButtonPressed => CurrentMouseState.MiddleButton == ButtonState.Pressed;

    public bool LeftClicked { get; private set; }

    public bool RightClicked { get; private set; }

    public bool MiddleClicked { get; private set; }

    public delegate void MouseClickEventHandler(int x, int y, MouseButton mouseButton);

    public event MouseClickEventHandler? MousePress;

    public event MouseClickEventHandler? MouseRelease;

    public event MouseClickEventHandler? MouseClick;

    public event MouseClickEventHandler? MouseDoubleClick;

    public InputService(Game game)
    {
        _game = game ?? throw new ArgumentNullException(nameof(game));
        CurrentMouseState = Mouse.GetState();
        PreviousMouseState = CurrentMouseState;
        CurrentKeyboardState = Keyboard.GetState();
        PreviousKeyboardState = CurrentKeyboardState;
    }

    public InputService()
        : this(Runtime.GameInstance)
    {
    }

    public bool IsKeyPressed(Keys key) => CurrentKeyboardState.IsKeyDown(key);

    public bool WasKeyPressed(Keys key) =>
        CurrentKeyboardState.IsKeyDown(key) && PreviousKeyboardState.IsKeyUp(key);

    public bool WasKeyReleased(Keys key) =>
        CurrentKeyboardState.IsKeyUp(key) && PreviousKeyboardState.IsKeyDown(key);

    public void Update(GameTime gameTime)
    {
        _elapsed += gameTime.ElapsedGameTime;
        LeftClicked = false;
        RightClicked = false;
        MiddleClicked = false;

        PreviousKeyboardState = CurrentKeyboardState;
        CurrentKeyboardState = Keyboard.GetState();
        PreviousMouseState = CurrentMouseState;
        CurrentMouseState = Mouse.GetState();

        if (!_game.IsActive || !IsInsideWindow(CurrentMouseState.Position))
        {
            CancelClicks();
            return;
        }

        ProcessButton(MouseButton.Left, PreviousMouseState.LeftButton, CurrentMouseState.LeftButton);
        ProcessButton(MouseButton.Right, PreviousMouseState.RightButton, CurrentMouseState.RightButton);
        ProcessButton(MouseButton.Middle, PreviousMouseState.MiddleButton, CurrentMouseState.MiddleButton);
    }

    private void ProcessButton(MouseButton button, ButtonState previous, ButtonState current)
    {
        int index = (int)button;
        if (previous == current)
        {
            return;
        }

        if (current == ButtonState.Pressed)
        {
            _clickTrackers[index] = true;
            _pressPositions[index] = MousePosition;
            MousePress?.Invoke(MousePosition.X, MousePosition.Y, button);
            return;
        }

        MouseRelease?.Invoke(MousePosition.X, MousePosition.Y, button);
        bool isClick = _clickTrackers[index]
            && Vector2.Distance(_pressPositions[index].ToVector2(), MousePosition.ToVector2()) <= ClickMovementTolerance;
        _clickTrackers[index] = false;

        if (!isClick)
        {
            return;
        }

        SetClicked(button);
        MouseClick?.Invoke(MousePosition.X, MousePosition.Y, button);

        bool isDoubleClick = _hasPreviousClick[index]
            && _elapsed - _lastClickTimes[index] <= DoubleClickWindow
            && Vector2.Distance(_lastClickPositions[index].ToVector2(), MousePosition.ToVector2()) <= ClickMovementTolerance;

        if (isDoubleClick)
        {
            MouseDoubleClick?.Invoke(MousePosition.X, MousePosition.Y, button);
            _hasPreviousClick[index] = false;
            return;
        }

        _hasPreviousClick[index] = true;
        _lastClickTimes[index] = _elapsed;
        _lastClickPositions[index] = MousePosition;
    }

    private bool IsInsideWindow(Point point)
    {
        Rectangle bounds = _game.Window.ClientBounds;
        return new Rectangle(0, 0, bounds.Width, bounds.Height).Contains(point);
    }

    private void SetClicked(MouseButton button)
    {
        LeftClicked = button == MouseButton.Left;
        RightClicked = button == MouseButton.Right;
        MiddleClicked = button == MouseButton.Middle;
    }

    private void CancelClicks()
    {
        Array.Fill(_clickTrackers, false);
    }

    public Vector2 GetMouseVelocity() => MouseVelocity;
}
