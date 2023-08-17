/*
* This file is part of the UnityComponents repository authored by Ovahlord (https://github.com/Ovahlord/UnityComponents)
* All components in this repository are royalty free and can be used for commercial purposes. Enjoy.
*/

// This file contains a bunch of helpful methods and code pieces that have been discovered during researching several features and mechanics.

// Detecting gamepads when using the InputSystem
// Gamepads do return normalized vectors while Mouse and Keyboard return actual deltas for look input
InputUser.onChange += (InputUser user, InputUserChange change, InputDevice device) =>
{
    if (change == InputUserChange.ControlSchemeChanged)
        _isUsingGamepad = user.controlScheme.Value.name.Equals("Gamepad");
};
