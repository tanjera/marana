using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Terminal.Gui;

namespace Marana.GUI {

    internal class Utility {

        public delegate void OnButton();

        public static Dialog CreateDialog_NotificationOkay(string message, int width, int height, Window window) {
            Dialog dialog = new Dialog("Notification", width, height) { X = Pos.Center(), Y = Pos.Center() };
            Button btnSaved = new Button("Okay") { X = Pos.Center(), Y = Pos.Center() + (height / 2) - 1 };

            btnSaved.Clicked += () => { window.Remove(dialog); };

            dialog.Add(new Label(message) { X = Pos.Center(), Y = Pos.Center() });
            dialog.Add(btnSaved);

            return dialog;
        }

        public static Dialog CreateDialog_OptionYesNo(string message, int width, int height, Window window, OnButton onNo, OnButton onYes) {
            Dialog dialog = new Dialog("Notification", width, height) { X = Pos.Center(), Y = Pos.Center() };
            Button btnNo = new Button("No") { X = Pos.Center() - (width / 3), Y = Pos.Center() + (height / 2) - 1 };
            Button btnYes = new Button("Yes") { X = Pos.Center() + (width / 3) - 7, Y = Pos.Center() + (height / 2) - 1 };

            btnNo.Clicked += () => {
                window.Remove(dialog);
                onNo();
            };

            btnYes.Clicked += () => {
                window.Remove(dialog);
                onYes();
            };

            dialog.Add(new Label(message) { X = Pos.Center(), Y = Pos.Center(), TextAlignment = TextAlignment.Centered });
            dialog.Add(btnNo, btnYes);

            return dialog;
        }

        public static Window CreateWindow(string id, string title) {
            Window window = new Window(title) {
                Id = id,
                X = 0,
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Fill() - 1
            };

            Application.Top.Add(window);

            return window;
        }

        public static Window SelectWindow(Main.WindowTypes windowType) {
            Window window = Application.Top.Subviews.Where(s => s.Id == windowType.ToString()).First() as Window;

            foreach (View view in Application.Top.Subviews.Where(s => s is Window && s != window))
                view.Visible = false;

            window.Visible = true;
            Application.Top.BringSubviewToFront(window);

            return window;
        }
    }
}