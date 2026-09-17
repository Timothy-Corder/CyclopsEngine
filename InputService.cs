using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace CyclopsEngine;

public enum MouseButton
{
    /// <summary>
    /// The left mouse button.
    /// </summary>
    Left,
    /// <summary>
    /// The right mouse button.
    /// </summary>
    Right,
    /// <summary>
    /// The middle mouse button.
    /// </summary>
    Middle
}

/// <summary>
/// Tracks keyboard and mouse state and raises mouse press, release, click, and double-click events.
/// </summary>
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

    /// <summary>
    /// The mouse state captured during the previous update.
    /// </summary>
    public MouseState PreviousMouseState { get; private set; }

    /// <summary>
    /// The most recently captured mouse state.
    /// </summary>
    public MouseState CurrentMouseState { get; private set; }

    /// <summary>
    /// The keyboard state captured during the previous update.
    /// </summary>
    public KeyboardState PreviousKeyboardState { get; private set; }

    /// <summary>
    /// The most recently captured keyboard state.
    /// </summary>
    public KeyboardState CurrentKeyboardState { get; private set; }

    /// <summary>
    /// The current mouse position relative to the game window.
    /// </summary>
    public Point MousePosition => CurrentMouseState.Position;

    /// <summary>
    /// The mouse movement since the previous update.
    /// </summary>
    public Vector2 MouseVelocity => new(
        CurrentMouseState.X - PreviousMouseState.X,
        CurrentMouseState.Y - PreviousMouseState.Y);

    /// <summary>
    /// Whether the left mouse button is currently pressed.
    /// </summary>
    public bool LeftButtonPressed => CurrentMouseState.LeftButton == ButtonState.Pressed;

    /// <summary>
    /// Whether the right mouse button is currently pressed.
    /// </summary>
    public bool RightButtonPressed => CurrentMouseState.RightButton == ButtonState.Pressed;

    /// <summary>
    /// Whether the middle mouse button is currently pressed.
    /// </summary>
    public bool MiddleButtonPressed => CurrentMouseState.MiddleButton == ButtonState.Pressed;

    /// <summary>
    /// Whether a left-button click was completed during the current update.
    /// </summary>
    public bool LeftClicked { get; private set; }

    /// <summary>
    /// Whether a right-button click was completed during the current update.
    /// </summary>
    public bool RightClicked { get; private set; }

    /// <summary>
    /// Whether a middle-button click was completed during the current update.
    /// </summary>
    public bool MiddleClicked { get; private set; }

    /// <summary>
    /// Represents a handler for a mouse action at a window position.
    /// </summary>
    public delegate void MouseClickEventHandler(int x, int y, MouseButton mouseButton);

    /// <summary>
    /// Occurs when a mouse button is pressed inside the active game window.
    /// </summary>
    public event MouseClickEventHandler? MousePress;

    /// <summary>
    /// Occurs when a mouse button is released inside the active game window.
    /// </summary>
    public event MouseClickEventHandler? MouseRelease;

    /// <summary>
    /// Occurs when a mouse button is pressed and released within the movement tolerance.
    /// </summary>
    public event MouseClickEventHandler? MouseClick;

    /// <summary>
    /// Occurs when two qualifying clicks happen within the configured time and movement tolerances.
    /// </summary>
    public event MouseClickEventHandler? MouseDoubleClick;

    /// <summary>
    /// Creates an input service for the specified game instance and captures its initial input state.
    /// </summary>
    public InputService(Game game)
    {
        _game = game ?? throw new ArgumentNullException(nameof(game));
        CurrentMouseState = Mouse.GetState();
        PreviousMouseState = CurrentMouseState;
        CurrentKeyboardState = Keyboard.GetState();
        PreviousKeyboardState = CurrentKeyboardState;
    }

    /// <summary>
    /// Creates an input service for the active runtime game instance.
    /// </summary>
    public InputService()
        : this(Runtime.GameInstance)
    {
    }

    /// <summary>
    /// Determines whether a key is currently pressed.
    /// </summary>
    public bool IsKeyPressed(Keys key) => CurrentKeyboardState.IsKeyDown(key);

    /// <summary>
    /// Determines whether a key became pressed during the current update.
    /// </summary>
    public bool WasKeyPressed(Keys key) =>
        CurrentKeyboardState.IsKeyDown(key) && PreviousKeyboardState.IsKeyUp(key);

    /// <summary>
    /// Determines whether a key was released during the current update.
    /// </summary>
    public bool WasKeyReleased(Keys key) =>
        CurrentKeyboardState.IsKeyUp(key) && PreviousKeyboardState.IsKeyDown(key);

    /// <summary>
    /// Captures current input state and processes mouse-button transitions and click gestures.
    /// </summary>
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

    /// <summary>
    /// Gets the mouse movement since the previous update.
    /// </summary>
    public Vector2 GetMouseVelocity() => MouseVelocity;
}
