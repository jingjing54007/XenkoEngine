﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Threading;
using SiliconStudio.Core.Annotations;
using SiliconStudio.Presentation.Resources;
using SiliconStudio.Presentation.Services;

namespace SiliconStudio.Presentation.Windows
{
    public static class DialogHelper
    {
        [NotNull]
        public static Task<MessageBoxResult> MessageBox([NotNull] IDispatcherService dispatcher, string message, string caption, MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None, WindowOwner owner = WindowOwner.LastModal)
        {
            return dispatcher.InvokeTask(() => Windows.MessageBox.Show(owner, message, caption, button, image));
        }

        [NotNull]
        public static Task<MessageBoxResult> MessageBox([NotNull] IDispatcherService dispatcher, string message, string caption, IEnumerable<DialogButtonInfo> buttons, MessageBoxImage image = MessageBoxImage.None, WindowOwner owner = WindowOwner.LastModal)
        {
            return dispatcher.InvokeTask(() => Windows.MessageBox.Show(owner, message, caption, buttons, image));
        }

        [NotNull]
        public static Task<CheckedMessageBoxResult> CheckedMessageBox([NotNull] IDispatcherService dispatcher, string message, string caption, bool? isChecked, MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None, WindowOwner owner = WindowOwner.LastModal)
        {
            return dispatcher.InvokeTask(() => Windows.CheckedMessageBox.Show(owner, message, caption, button, image, Strings.DontAskMeAgain, isChecked));
        }

        [NotNull]
        public static Task<CheckedMessageBoxResult> CheckedMessageBox([NotNull] IDispatcherService dispatcher, string message, string caption, bool? isChecked, string checkboxMessage, MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None, WindowOwner owner = WindowOwner.LastModal)
        {
            return dispatcher.InvokeTask(() => Windows.CheckedMessageBox.Show(owner, message, caption, button, image, checkboxMessage, isChecked));
        }

        public static MessageBoxResult BlockingMessageBox([NotNull] IDispatcherService dispatcher, string message, string caption, MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None, WindowOwner owner = WindowOwner.LastModal)
        {
            return PushFrame(dispatcher, () => MessageBox(dispatcher, message, caption, button, image, owner));
        }

        public static MessageBoxResult BlockingMessageBox([NotNull] IDispatcherService dispatcher, string message, string caption, IEnumerable<DialogButtonInfo> buttons, MessageBoxImage image = MessageBoxImage.None, WindowOwner owner = WindowOwner.LastModal)
        {
            return PushFrame(dispatcher, () => MessageBox(dispatcher, message, caption, buttons, image, owner));
        }

        public static CheckedMessageBoxResult BlockingCheckedMessageBox([NotNull] IDispatcherService dispatcher, string message, string caption, bool? isChecked, MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None, WindowOwner owner = WindowOwner.LastModal)
        {
            return PushFrame(dispatcher, () => CheckedMessageBox(dispatcher, message, caption, isChecked, button, image, owner));
        }

        public static CheckedMessageBoxResult BlockingCheckedMessageBox([NotNull] IDispatcherService dispatcher, string message, string caption, bool? isChecked, string checkboxMessage, MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None, WindowOwner owner = WindowOwner.LastModal)
        {
            return PushFrame(dispatcher, () => CheckedMessageBox(dispatcher, message, caption, isChecked, checkboxMessage, button, image, owner));
        }

        private static TResult PushFrame<TResult>([NotNull] IDispatcherService dispatcher, Func<Task<TResult>> task)
        {
            return dispatcher.Invoke(() =>
            {
                var frame = new DispatcherFrame();
                var frameTask = task().ContinueWith(x => { frame.Continue = false; return x.Result; });
                ComponentDispatcher.PushModal();
                Dispatcher.PushFrame(frame);
                ComponentDispatcher.PopModal();
                return frameTask.Result;
            });
        }
    }
}
