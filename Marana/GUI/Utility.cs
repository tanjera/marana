using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Terminal.Gui;

namespace Marana.GUI {

    internal class Utility {

        public delegate void OnButton();

        public static Dialog DialogNotification_Okay(string message, int width, int height, Window window) {
            Dialog dialog = new Dialog("Notification", width, height) { X = Pos.Center(), Y = Pos.Center() };
            Button btnSaved = new Button("Okay") { X = Pos.Center(), Y = Pos.Center() + (height / 2) - 1 };

            btnSaved.Clicked += () => { window.Remove(dialog); };

            dialog.Add(new Label(message) { X = Pos.Center(), Y = Pos.Center() });
            dialog.Add(btnSaved);

            return dialog;
        }

        public static Dialog DialogOption_YesNo(string message, int width, int height, Window window, OnButton onNo, OnButton onYes) {
            Dialog dialog = new Dialog("Notification", width, height) { X = Pos.Center(), Y = Pos.Center() };
            Button btnNo = new Button("No") { X = Pos.Center() - (width / 3), Y = Pos.Center() + (height / 2) - 1 };
            Button btnYes = new Button("Yes") { X = Pos.Center() + (width / 3) - 7, Y = Pos.Center() + (height / 2) - 1 };

            btnNo.Clicked += () => {
                onNo();
                window.Remove(dialog);
            };

            btnYes.Clicked += () => {
                onYes();
                window.Remove(dialog);
            };

            dialog.Add(new Label(message) { X = Pos.Center(), Y = Pos.Center() });
            dialog.Add(btnNo, btnYes);

            return dialog;
        }

        public static Window SetWindow(string title) {
            View[] views = Application.Top.Subviews.ToArray();

            for (int i = views.Length - 1; i >= 0; i--) {
                View view = Application.Top.Subviews.ToArray()[i];

                if (!(view is MenuBar)) {
                    Application.Top.Remove(view);
                }
            }

            Window window = new Window(title) {
                X = 0,
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Fill() - 1
            };

            Application.Top.Add(window);

            return window;
        }
    }
}