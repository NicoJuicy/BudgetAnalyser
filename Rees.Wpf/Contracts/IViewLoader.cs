﻿namespace Rees.Wpf.Contracts;

/// <summary>
///     A means to load a view for displaying purposes. Used by a controller in Mvvm or Mvp to trigger display of a view.
/// </summary>
public interface IViewLoader
{
    /// <summary>
    ///     Show the view in a dialog manner.
    /// </summary>
    /// <param name="context">The model or context the view can use for binding or reference purposes.</param>
    /// <returns>
    ///     Used to indicate how the user closed the dialog.  Can be used to determine the difference between cancelling
    ///     or confirmation.
    /// </returns>
    bool? ShowDialog(object context);
}
