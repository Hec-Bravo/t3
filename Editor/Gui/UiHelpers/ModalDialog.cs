﻿using System.Numerics;
using ImGuiNET;

namespace T3.Editor.Gui.UiHelpers
{
    /// <summary>
    /// Framework for rendering modal dialogs.
    ///
    /// Should be implemented like...
    ///
    /// void SomeClass {
    ///     void Draw() {
    ///         _someDialog.Draw();
    /// 
    ///        if(ImGui.Button()) {
    ///            _someDialog.ShowNextFrame();
    ///        }
    ///     }
    /// }
    /// 
    /// void SomeDialog()
    /// {
    ///     void Draw()
    ///     {
    ///         if(BeginDialog("myTitle"))
    ///         {    
    /// 
    ///           // draw your content...
    /// 
    ///           EndDialogContent();
    ///         }   
    ///         EndDialog();
    ///     }
    /// }
    /// 
    /// </summary>
    public abstract class ModalDialog
    {
        public void ShowNextFrame()
        {
            _shouldShowNextFrame = true;
        }

        protected bool BeginDialog(string title)
        {
            if (_shouldShowNextFrame)
            {
                _shouldShowNextFrame = false;
                ImGui.OpenPopup(title);
            }


            ImGui.SetNextWindowSize(DialogSize, ImGuiCond.FirstUseEver);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(Padding, Padding));

            if (!ImGui.BeginPopupModal(title))
                return false;
            
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, ItemSpacing);
            return true;
        }

        /// <summary>
        /// Only call if BeginDialog returned true
        /// </summary>
        protected static void EndDialogContent()
        {
            ImGui.PopStyleVar();
            ImGui.EndPopup();
        }

        /// <summary>
        /// Call always
        /// </summary>
        protected static void EndDialog()
        {
            ImGui.PopStyleVar();
        }

        private bool _shouldShowNextFrame;
        protected Vector2 DialogSize = new(500, 250);
        protected Vector2 ItemSpacing = new Vector2(4, 10);
        protected float Padding = 20;
    }
}