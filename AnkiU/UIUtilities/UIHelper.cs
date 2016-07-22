﻿/*
Copyright (C) 2016 Anki Universal Team <ankiuniversal@outlook.com>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as
published by the Free Software Foundation, either version 3 of the
License, or (at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using AnkiU.Interfaces;
using OxyPlot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Devices.Input;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;

namespace AnkiU.UIUtilities
{
    static class UIHelper
    {
        private static readonly Regex DigitsOnly = new Regex(@"[^\d]", RegexOptions.Compiled);
        private static readonly Regex DigitsAndWhitespaceOnly = new Regex(@"[^\d ]", RegexOptions.Compiled);

        public static readonly char[] ILLEGAL_NAME_CHAR = new char[] { '`', '~', '!', '@', '#', '$', '%', '^', '&', '*',
                                                                        '(', ')', '/', '<', '>', '.', ':', ';', '"', '\'',
                                                                        '[', ']', '{', '}', '=', '-', '+', ',', '|', '\\' };

        private const int numberRangeMin = (int)Windows.System.VirtualKey.Number0;
        private const int numberRangeMax = (int)Windows.System.VirtualKey.Number9;

        private const int numberPadRangeMin = (int)Windows.System.VirtualKey.NumberPad0;
        private const int numberPadRangeMax = (int)Windows.System.VirtualKey.NumberPad9;

        private static SolidColorBrush darkerBrush = Application.Current.Resources["DarkerGray"] as SolidColorBrush;
        public static SolidColorBrush DarkerBrush { get { return darkerBrush; } }
        private static SolidColorBrush contentNightModeBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 32, 32, 32));
        public static SolidColorBrush ContentNightModeBrush { get { return contentNightModeBrush; } }
        public static Windows.UI.Color ContentNightModeColor { get { return contentNightModeBrush.Color; } }
        private static Windows.UI.Color defaultInkColorDay = Windows.UI.Color.FromArgb(255, 11, 96, 181);
        public static Windows.UI.Color DefaultInkColorDay { get { return defaultInkColorDay; } }
        private static Windows.UI.Color defaultInkColorNight = Windows.UI.Color.FromArgb(255, 0, 79, 159);
        public static Windows.UI.Color DefaultInkColorNight { get { return defaultInkColorNight; }  }
        public const double DEFAULT_INK_SIZE = 3;

        private static bool IsNumberKey(int key)
        {
            return (key <= numberRangeMax && key >= numberRangeMin) ||
                           (key <= numberPadRangeMax && key >= numberPadRangeMin);
        }

        public static string StripNonDigit(this string text)
        {
            return DigitsOnly.Replace(text, "");
        }

        public static string StripNonDigitOrWhiteSpace(this string text)
        {
            return DigitsAndWhitespaceOnly.Replace(text, "");
        }

        public static childItem GetChildrenInDataTemplate<childItem>(DependencyObject containter, string templateItemName) where childItem : FrameworkElement
        {
            var children = AllChildren<childItem>(containter);
            var child = children.OfType<childItem>().First(x => x.Name.Equals(templateItemName));
            return child;
        }

        public static Parent GetParent<Parent>(FrameworkElement element) where Parent : FrameworkElement
        {
            var parent = VisualTreeHelper.GetParent(element) as Parent;
            if (parent == null)
                throw new Exception("The parent is not stored in the specified parent type");
            return parent;
        }

        public static List<childItem> AllChildren<childItem>(DependencyObject parent) where childItem : DependencyObject
        {
            var childList = new List<childItem>();
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                var item = child as childItem;
                if (item != null)
                    childList.Add(item);

                childList.AddRange(AllChildren<childItem>(child));
            }
            return childList;
        }

        public static async Task<StorageFile> OpenFilePicker(string tokenName, PickerLocationId location, params string[] fileTypes)
        {
            var filePicker = new FileOpenPicker();
            filePicker.SuggestedStartLocation = location;
            foreach (var type in fileTypes)
                filePicker.FileTypeFilter.Add(type);
            var filePick = await filePicker.PickSingleFileAsync();
            if (filePick != null)
            {
                Windows.Storage.AccessCache.StorageApplicationPermissions.
                FutureAccessList.AddOrReplace(tokenName, filePick);
                return filePick;
            }
            else
                return null;
        }

        public static async Task<StorageFile> OpenFilePicker(string tokenName, params string[] fileTypes)
        {
            return await OpenFilePicker(tokenName, PickerLocationId.DocumentsLibrary, fileTypes);
        }


        [Conditional("DEBUG")]
        public static void ThrowJavascriptError(int HResult, [CallerMemberName] string functionName = null)
        {
            switch (HResult)
            {
                case unchecked((int)0x80020006):
                    throw new Exception("JavaScript: There is no function called " + functionName);

                case unchecked((int)0x80020101):
                    throw new Exception("JavaScript: A JavaScript error or exception occured while executing the function " + functionName);

                case unchecked((int)0x800a138a):
                    throw new Exception("JavaScript: " + functionName + " is not a function");
                default:
                    throw new Exception("JavaScript: " + functionName + ": Unknown error! " + HResult);
            }
        }

        public static async Task ShowMessageDialog(string content, string title = "")
        {
            MessageDialog dialog = new MessageDialog(content, title);
            await dialog.ShowAsync();
        }

        public static async Task<bool> AskUserConfirmation(string content, string title = "")
        {
            MessageDialog dialog = new MessageDialog(content, title);
            bool isContinue = false;
            dialog.Commands.Add(new UICommand("Yes", (command) =>
            {
                isContinue = true;
            }));
            dialog.Commands.Add(new UICommand("No", (command) =>
            {
                isContinue = false;
            }));
            dialog.DefaultCommandIndex = 1;
            await dialog.ShowAsync();
            return isContinue;
        }

        public static string GetDeviceFamily()
        {
            return Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily;
        }

        public static bool IsMobileDevice()
        {
            return Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile";
        }

        public static bool IsHasPhysicalKeyboard()
        {
            KeyboardCapabilities keyboardCapabilities = new Windows.Devices.Input.KeyboardCapabilities();            
            return keyboardCapabilities.KeyboardPresent != 0 ;
        }

        public static bool IsHasPhysicalMouse()
        {
            MouseCapabilities mouseCap = new Windows.Devices.Input.MouseCapabilities();            
            return mouseCap.MousePresent != 0;
        }

        public static bool IsHasPen()
        {
            var pointerDevices = Windows.Devices.Input.PointerDevice.GetPointerDevices();
            foreach(var pointer in pointerDevices)
            {
                if (pointer.PointerDeviceType == PointerDeviceType.Pen)
                    return true;
            }
            return false;
        }

        public static StringBuilder GetDateTimeStringForName()
        {
            StringBuilder name = new StringBuilder(DateTimeOffset.Now.UtcDateTime.ToString());
            name = name.Replace("\\", "_");
            name = name.Replace("/", "_");
            name = name.Replace(":", "_");
            return name;
        }

        public static async Task<bool> CheckValidName(string name, IEnumerable<IName> existing, string errorMessage)
        {
            if (String.IsNullOrWhiteSpace(name))
            {
                await ShowMessageDialog("Name cannot be empty. Please enter a valid name.");
                return false;
            }
            foreach (var ex in existing)
            {
                if (ex.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase))
                {
                    await ShowMessageDialog(errorMessage);
                    return false;
                }
            }
            return true;
        }

        public static void ToggleNightLight(bool isNight, Page userControl)
        {            
            if (isNight)
            {
                userControl.Background = Application.Current.Resources["DarkerGray"] as SolidColorBrush;
                userControl.Foreground = Application.Current.Resources["ForeGroundLight"] as SolidColorBrush;
            }
            else
            {
                userControl.Background = Application.Current.Resources["BackgroundNormal"] as SolidColorBrush;
                userControl.Foreground = new SolidColorBrush(Windows.UI.Colors.Black);
            }
        }

        public static void ChangePlotModelToNight(PlotModel ChartModel)
        {
            foreach (var a in ChartModel.Axes)
            {
                a.TextColor = OxyColors.White;
                a.AxislineColor = OxyColors.White;
                a.TicklineColor = OxyColors.White;
                a.MajorGridlineColor = OxyColors.White;
                a.MinorGridlineColor = OxyColors.White;
            }
            ChartModel.TextColor = OxyColors.White;
            ChartModel.TitleColor = OxyColors.White;
            ChartModel.SubtitleColor = OxyColors.White;
            ChartModel.PlotAreaBorderColor = OxyColors.White;

            ChartModel.PlotAreaBackground = OxyColors.Black;            
            ChartModel.Background = OxyColors.Black;
        }

        public static void ChangePlotModelToDay(PlotModel ChartModel)
        {
            foreach (var a in ChartModel.Axes)
            {
                a.TextColor = OxyColors.Black;
                a.AxislineColor = OxyColors.Black;
                a.TicklineColor = OxyColors.Black;
                a.MajorGridlineColor = OxyColors.Black;
                a.MinorGridlineColor = OxyColors.Black;
            }
            ChartModel.TextColor = OxyColors.Black;
            ChartModel.TitleColor = OxyColors.Black;
            ChartModel.SubtitleColor = OxyColors.Black;
            ChartModel.PlotAreaBorderColor = OxyColors.Black;

            ChartModel.PlotAreaBackground = OxyColors.White;            
            ChartModel.Background = OxyColors.White;
        }

        public static async Task<IRandomAccessStream> RenderToRandomAccessStream(this Windows.UI.Xaml.UIElement element)
        {
            RenderTargetBitmap rtb = new RenderTargetBitmap();
            await rtb.RenderAsync(element);

            var pixelBuffer = await rtb.GetPixelsAsync();
            var pixels = pixelBuffer.ToArray();

            // Useful for rendering in the correct DPI
            var displayInformation = DisplayInformation.GetForCurrentView();

            var stream = new InMemoryRandomAccessStream();
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
            encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                                 BitmapAlphaMode.Premultiplied,
                                 (uint)rtb.PixelWidth,
                                 (uint)rtb.PixelHeight,
                                 displayInformation.RawDpiX,
                                 displayInformation.RawDpiY,
                                 pixels);

            await encoder.FlushAsync();
            stream.Seek(0);

            return stream;
        }

        public static bool IsDeskTop()
        {
            return GetDeviceFamily() == "Windows.Desktop";
        }

        public static async Task<StorageFolder> OpenFolderPicker(string token)
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            folderPicker.FileTypeFilter.Add("*");
            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                Windows.Storage.AccessCache.StorageApplicationPermissions.
                FutureAccessList.AddOrReplace(token, folder);
            }
            return folder;
        }

        public static void AddToGridInFull(Grid mainGrid, FrameworkElement control)
        {
            control.VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Stretch;
            control.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch;
            if(mainGrid.ColumnDefinitions.Count > 0)
                Grid.SetColumnSpan(control, mainGrid.ColumnDefinitions.Count);
            if(mainGrid.RowDefinitions.Count > 0)
                Grid.SetRowSpan(control, mainGrid.RowDefinitions.Count);
            Grid.SetRow(control, 0);
            Grid.SetColumn(control, 0);
            mainGrid.Children.Add(control);
        }

        public static void SetStoryBoardTarget(ColorAnimationUsingKeyFrames animation, string targetName)
        {            
            animation.SetValue(Storyboard.TargetNameProperty, targetName);
        }

        public static void SetStoryBoardTarget(DoubleAnimation animation, string targetName)
        {
            animation.SetValue(Storyboard.TargetNameProperty, targetName);
        }

        public static void SetStoryBoardTarget(DoubleAnimationUsingKeyFrames animation, string targetName)
        {
            animation.SetValue(Storyboard.TargetNameProperty, targetName);
        }
    }
}