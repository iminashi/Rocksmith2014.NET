module DLCBuilder.Media

open Avalonia.Media
open Avalonia.Input

/// Icons from Material Design.
module Icons =
    let guitar = Geometry.Parse "M19.59,3H22V5H20.41L15.12,10.29L13.71,8.9L19.59,3M12,9C12.26,9 12.5,9.1 12.71,9.3L14.71,11.3C14.89,11.5 15,11.73 15,12L14.9,12.4L10.9,20.4C10.71,20.75 10.36,20.93 10,20.93C9.65,20.93 9.29,20.75 9.11,20.4L7.25,16.7L3.55,14.9C3.18,14.7 3,14.35 3,14C3,13.65 3.18,13.3 3.55,13.1L11.55,9.1C11.69,9 11.84,9 12,9M9.35,11.82L8.65,12.5L11.5,15.35L12.18,14.65L9.35,11.82M7.94,13.23L7.23,13.94L10.06,16.77L10.77,16.06L7.94,13.23Z"
    let pick = Geometry.Parse "M19,4.1C18.1,3.3 17,2.8 15.8,2.5C15.5,2.4 13.6,2 12.2,2C12.2,2 12.1,2 12,2C12,2 11.9,2 11.8,2C10.4,2 8.4,2.4 8.1,2.5C7,2.8 5.9,3.3 5,4.1C3,5.9 3,8.7 4,11C5,13.5 6.1,15.7 7.6,17.9C8.8,19.6 10.1,22 12,22C13.9,22 15.2,19.6 16.5,17.9C18,15.8 19.1,13.5 20.1,11C21,8.7 21,5.9 19,4.1Z"
    let microphone = Geometry.Parse "M12,2A3,3 0 0,1 15,5V11A3,3 0 0,1 12,14A3,3 0 0,1 9,11V5A3,3 0 0,1 12,2M19,11C19,14.53 16.39,17.44 13,17.93V21H11V17.93C7.61,17.44 5,14.53 5,11H7A5,5 0 0,0 12,16A5,5 0 0,0 17,11H19Z"
    let spotlight = Geometry.Parse "M2,6L7.09,8.55C6.4,9.5 6,10.71 6,12C6,13.29 6.4,14.5 7.09,15.45L2,18V6M6,3H18L15.45,7.09C14.5,6.4 13.29,6 12,6C10.71,6 9.5,6.4 8.55,7.09L6,3M22,6V18L16.91,15.45C17.6,14.5 18,13.29 18,12C18,10.71 17.6,9.5 16.91,8.55L22,6M18,21H6L8.55,16.91C9.5,17.6 10.71,18 12,18C13.29,18 14.5,17.6 15.45,16.91L18,21M12,8A4,4 0 0,1 16,12A4,4 0 0,1 12,16A4,4 0 0,1 8,12A4,4 0 0,1 12,8M12,10A2,2 0 0,0 10,12A2,2 0 0,0 12,14A2,2 0 0,0 14,12A2,2 0 0,0 12,10Z"
    let x = Geometry.Parse "M20 6.91L17.09 4L12 9.09L6.91 4L4 6.91L9.09 12L4 17.09L6.91 20L12 14.91L17.09 20L20 17.09L14.91 12L20 6.91Z"
    let check = Geometry.Parse "M9,20.42L2.79,14.21L5.62,11.38L9,14.77L18.88,4.88L21.71,7.71L9,20.42Z"
    let alertRound = Geometry.Parse "M11,15H13V17H11V15M11,7H13V13H11V7M12,2C6.47,2 2,6.5 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,20A8,8 0 0,1 4,12A8,8 0 0,1 12,4A8,8 0 0,1 20,12A8,8 0 0,1 12,20Z"
    let alertTriangle = Geometry.Parse "M12,2L1,21H23M12,6L19.53,19H4.47M11,10V14H13V10M11,16V18H13V16"

module Brushes =
    let lead = SolidColorBrush.Parse "#ff9242" :> ISolidColorBrush
    let rhythm = SolidColorBrush.Parse "#1ea334" :> ISolidColorBrush
    let bass = SolidColorBrush.Parse "#0383b5" :> ISolidColorBrush
    let vocals = Brushes.LightGray
    let jvocals = Brushes.PaleVioletRed
    let showlights = Brushes.Violet

module Cursors =
    let hand = Cursor(StandardCursorType.Hand)
    let arrow = Cursor(StandardCursorType.Arrow)
    let appStarting = Cursor(StandardCursorType.AppStarting)
