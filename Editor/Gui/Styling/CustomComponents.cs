using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.IO;
using T3.Core.Utils;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Styling
{
    internal static class CustomComponents
    {
        public static bool JogDial(string label, ref double delta, Vector2 size)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(1, 0.5f));
            var isActive = ImGui.Button(label + "###dummy", size);
            ImGui.PopStyleVar();
            var io = ImGui.GetIO();
            if (ImGui.IsItemActive())
            {
                var center = (ImGui.GetItemRectMin() + ImGui.GetItemRectMax()) * 0.5f;
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                ImGui.GetForegroundDrawList().AddCircle(center, 100, Color.Gray, 50);
                isActive = true;

                var pLast = io.MousePos - io.MouseDelta - center;
                var pNow = io.MousePos - center;
                var aLast = Math.Atan2(pLast.X, pLast.Y);
                var aNow = Math.Atan2(pNow.X, pNow.Y);
                delta = aLast - aNow;
                if (delta > 1.5)
                {
                    delta -= 2 * Math.PI;
                }
                else if (delta < -1.5)
                {
                    delta += 2 * Math.PI;
                }
            }

            return isActive;
        }

        /// <summary>Draw a splitter</summary>
        /// <remarks>
        /// Take from https://github.com/ocornut/imgui/issues/319#issuecomment-147364392
        /// </remarks>
        public static bool SplitFromBottom(ref float offsetFromBottom)
        {
            const float thickness = 3;
            var hasBeenDragged = false;

            var backupPos = ImGui.GetCursorPos();

            var size = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin();
            var contentMin = ImGui.GetWindowContentRegionMin() + ImGui.GetWindowPos();

            var pos = new Vector2(contentMin.X, contentMin.Y + size.Y - offsetFromBottom - thickness);
            ImGui.SetCursorScreenPos(pos);

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 1));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0, 0, 0, 1));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.5f, 0.5f, 0.5f, 1));

            ImGui.Button("##Splitter", new Vector2(-1, thickness));

            ImGui.PopStyleColor(3);

            // Disabled for now, since Setting MouseCursor wasn't working reliably
            // if (ImGui.IsItemHovered() )
            // {
            //     //ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNS);
            // }

            if (ImGui.IsItemActive())
            {
                if (Math.Abs(ImGui.GetIO().MouseDelta.Y) > 0)
                {
                    hasBeenDragged = true;
                    offsetFromBottom =
                        (offsetFromBottom - ImGui.GetIO().MouseDelta.Y)
                       .Clamp(0, size.Y - thickness);
                }
            }

            ImGui.SetCursorPos(backupPos);
            return hasBeenDragged;
        }

        public static bool ToggleButton(string label, ref bool isSelected, Vector2 size, bool trigger = false)
        {
            var wasSelected = isSelected;
            var clicked = false;
            if (isSelected)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, Color.Gray.Rgba);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Color.Gray.Rgba);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, Color.Gray.Rgba);
            }

            if (ImGui.Button(label, size) || trigger)
            {
                isSelected = !isSelected;
                clicked = true;
            }

            if (wasSelected)
            {
                ImGui.PopStyleColor(3);
            }

            return clicked;
        }

        public static bool ToggleIconButton(Icon icon, string label, ref bool isSelected, Vector2 size, bool trigger = false)
        {
            var clicked = false;

            var stateColor = isSelected ? new Color(1f, 1, 1f, 1f) : new Color(0, 0, 0, 1f);
            ImGui.PushStyleColor(ImGuiCol.Text, stateColor.Rgba);


            var padding = string.IsNullOrEmpty(label) ?  new Vector2(0.1f, 0.5f) :  new Vector2(0.5f, 0.5f);
            ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, padding);
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
            
            ImGui.PushFont(Icons.IconFont);

            if (ImGui.Button($"{(char)icon}##label", size))
            {
                isSelected = !isSelected;
                clicked = true;
            }

            ImGui.PopFont();
            
            ImGui.PopStyleVar(2);
            ImGui.PopStyleColor(1);

            return clicked;
        }

        public static bool IconButton(Icon icon, string label, Vector2 size)
        {
            ImGui.PushFont(Icons.IconFont);
            ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(0.5f, 0.5f));
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
            
            var clicked = ImGui.Button(""+(char)icon, size);

            ImGui.PopStyleVar(2);
            ImGui.PopFont();
            return clicked;
        }

        public static void ContextMenuForItem(Action drawMenuItems, string title = null, string id = "context_menu", ImGuiPopupFlags flags = ImGuiPopupFlags.MouseButtonRight)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(8, 8));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6, 6));

            if (ImGui.BeginPopupContextItem(id, flags))
            {
                if (title != null)
                {
                    ImGui.PushFont(Fonts.FontLarge);
                    ImGui.TextUnformatted(title);
                    ImGui.PopFont();
                }

                drawMenuItems?.Invoke();
                ImGui.EndPopup();
            }

            ImGui.PopStyleVar(2);
        }
        

        public static void DrawContextMenuForScrollCanvas(Action drawMenuContent, ref bool contextMenuIsOpen)
        {
            // This is a horrible hack to distinguish right mouse click from right mouse drag
            //var rightMouseDragDelta = (ImGui.GetIO().MouseClickedPos[1] - ImGui.GetIO().MousePos).Length();
            var wasDraggingRight = ImGui.GetMouseDragDelta(ImGuiMouseButton.Right).Length() > UserSettings.Config.ClickThreshold;

            if (!contextMenuIsOpen)
            {
                if (wasDraggingRight)
                    return;

                if (!ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup))
                    return;
            }

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(8, 8));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6, 6));

            if (ImGui.BeginPopupContextWindow("context_menu"))
            {
                ImGui.GetMousePosOnOpeningCurrentPopup();
                contextMenuIsOpen = true;

                drawMenuContent.Invoke();
                ImGui.EndPopup();
            }
            else
            {
                contextMenuIsOpen = false;
            }

            ImGui.PopStyleVar(2);
        }

        public static bool DisablableButton(string label, bool isEnabled, bool enableTriggerWithReturn = false)
        {
            if (isEnabled)
            {
                ImGui.PushFont(Fonts.FontBold);
                if (ImGui.Button(label)
                    || (enableTriggerWithReturn && ImGui.IsKeyPressed((ImGuiKey)Key.Return)))
                {
                    ImGui.PopFont();
                    return true;
                }

                ImGui.PopFont();
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.3f, 0.3f, 0.1f));
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 1, 1, 0.15f));
                ImGui.Button(label);
                ImGui.PopStyleColor(2);
            }

            return false;
        }

        public static void HelpText(string text)
        {
            ImGui.PushFont(Fonts.FontSmall);
            ImGui.PushStyleColor(ImGuiCol.Text, Color.Gray.Rgba);
            ImGui.TextUnformatted(text);
            ImGui.PopStyleColor();
            ImGui.PopFont();
        }

        /// <summary>
        /// A small label that can be used to structure context menus
        /// </summary>
        public static void HintLabel(string label)
        {
            ImGui.PushFont(Fonts.FontSmall);
            ImGui.PushStyleColor(ImGuiCol.Text, Color.Gray.Rgba);
            ImGui.TextUnformatted(label);
            ImGui.PopStyleColor();
            ImGui.PopFont();
        }

        public static void FillWithStripes(ImDrawListPtr drawList, ImRect areaOnScreen, float patternWidth = 16)
        {
            drawList.PushClipRect(areaOnScreen.Min, areaOnScreen.Max, true);
            var lineColor = new Color(0f, 0f, 0f, 0.2f);
            var stripeOffset = GraphCanvas.Current == null ? patternWidth : (patternWidth / 2 * GraphCanvas.Current.Scale.X);
            var lineWidth = stripeOffset / 2.7f;

            var h = areaOnScreen.GetHeight();
            var stripeCount = (int)((areaOnScreen.GetWidth() + h + 3 * lineWidth) / stripeOffset);
            var p = areaOnScreen.Min - new Vector2(h + lineWidth, +lineWidth);
            var offset = new Vector2(h + 2 * lineWidth,
                                     h + 2 * lineWidth);

            for (var i = 0; i < stripeCount; i++)
            {
                drawList.AddLine(p, p + offset, lineColor, lineWidth);
                p.X += stripeOffset;
            }

            drawList.PopClipRect();
        }

        public static bool EmptyWindowMessage(string message, string buttonLabel = null)
        {
            var center = (ImGui.GetWindowContentRegionMax() + ImGui.GetWindowContentRegionMin()) / 2 + ImGui.GetWindowPos();
            var lines = message.Split('\n').ToArray();

            var lineCount = lines.Length;
            if (!string.IsNullOrEmpty(buttonLabel))
                lineCount++;
            
            var textLineHeight = ImGui.GetTextLineHeight();
            var y = center.Y - lineCount * textLineHeight / 2;
            var drawList = ImGui.GetWindowDrawList();

            var emptyMessageColor = new Color(0.3f);

            foreach (var line in lines)
            {
                var textSize = ImGui.CalcTextSize(line);
                var position = new Vector2(center.X - textSize.X / 2, y);
                drawList.AddText(position, emptyMessageColor, line);
                y += textLineHeight;
            }
            
            if (!string.IsNullOrEmpty(buttonLabel))
            {
                y += 10;
                var style = ImGui.GetStyle(); 
                var textSize = ImGui.CalcTextSize(buttonLabel) + style.FramePadding;
                var position = new Vector2(center.X - textSize.X / 2, y);
                ImGui.SetCursorScreenPos(position);
                return ImGui.Button(buttonLabel);
            }
            return false;
        }


        public static void TooltipForLastItem(string message, string additionalNotes = null, bool useHoverDelay = true)
        {
            if (!ImGui.IsAnyItemHovered())
            {
                _hoverStartTime = -1;
                return;
            }

            if (!ImGui.IsItemHovered())
                return;

            if (_hoverStartTime <= 0)
                _hoverStartTime = ImGui.GetTime();

            var hoverDuration = ImGui.GetTime() - _hoverStartTime;
            if (useHoverDelay && !(hoverDuration > UserSettings.Config.TooltipDelay))
                return;

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(5, 5));
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(300);
            ImGui.TextUnformatted(message);
            if (!string.IsNullOrEmpty(additionalNotes))
            {
                ImGui.TextColored(Color.Gray, additionalNotes);
            }
            ImGui.PopTextWrapPos();

            ImGui.EndTooltip();
            ImGui.PopStyleVar();
        }

        private static double _hoverStartTime;

        public static bool DrawSegmentedToggle(ref int currentIndex, List<string> options)
        {
            var changed = false;
            for (var index = 0; index < options.Count; index++)
            {
                var isActive = currentIndex == index;
                var option = options[index];

                ImGui.SameLine(0);
                ImGui.PushFont(isActive ? Fonts.FontBold : Fonts.FontNormal);
                ImGui.PushStyleColor(ImGuiCol.Button, Color.Transparent.Rgba);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Color.White.Fade(0.1f).Rgba);
                ImGui.PushStyleColor(ImGuiCol.Text, isActive ? Color.White : Color.White.Fade(0.5f).Rgba);

                if (ImGui.Button(option))
                {
                    if (!isActive)
                    {
                        currentIndex = index;
                        changed = true;
                    }
                }

                ImGui.PopFont();
                ImGui.PopStyleColor(3);
            }

            return changed;
        }


        public static bool DrawCheckboxParameter(string label, ref bool value, string tooltip= null)
        {
            var modified = ImGui.Checkbox(label, ref value);
            if (!string.IsNullOrEmpty(tooltip))
            {
                TooltipForLastItem(tooltip);
            }
            return modified;
        }
        
        public static bool DrawFloatParameter(string label, ref float value, float min = float.NegativeInfinity, float max = float.PositiveInfinity, float scale = 0.01f, bool clamp = false, string tooltip= null)
        {
            var labelSize = ImGui.CalcTextSize(label);
            const float leftPadding = 200;
            var p = ImGui.GetCursorPos();
            ImGui.SetCursorPosX(MathF.Max(leftPadding - labelSize.X, 0) + 10);
            ImGui.AlignTextToFramePadding();
            
            ImGui.TextUnformatted(label);
            ImGui.SetCursorPos(p);

            ImGui.SameLine();
            ImGui.SetCursorPosX(leftPadding + 20);
            ImGui.PushID(label);
            var size = new Vector2(150, ImGui.GetFrameHeight());
            var result = SingleValueEdit.Draw(ref value, size, min, max, clamp, scale);
            ImGui.PopID();
            var modified = (result & InputEditStateFlags.Modified) != InputEditStateFlags.Nothing;

            if (!string.IsNullOrEmpty(tooltip))
            {
                TooltipForLastItem(tooltip);
            }
            
            return modified;
        }
        
        public static bool DrawIntParameter(string label, ref int value, int min = int.MinValue, int max = int.MaxValue, float scale = 1)
        {
            var labelSize = ImGui.CalcTextSize(label);
            const float leftPadding = 200;
            var p = ImGui.GetCursorPos();
            ImGui.SetCursorPosX(MathF.Max(leftPadding - labelSize.X, 0) + 10);
            ImGui.AlignTextToFramePadding();
            
            ImGui.TextUnformatted(label);

            ImGui.SetCursorPos(p);

            ImGui.SameLine();
            ImGui.SetCursorPosX(leftPadding + 20);

            ImGui.PushID(label);
            var size = new Vector2(150, ImGui.GetFrameHeight());
            var result = SingleValueEdit.Draw(ref value, size, min, max, true, scale);
            ImGui.PopID();
            var modified = (result & InputEditStateFlags.Modified) != InputEditStateFlags.Nothing;
            return modified;
        }

        public static bool DrawStringParameter(string label, 
                                               ref string value, 
                                               string placeHolder = null, 
                                               string warning=null,  
                                               FileOperations.FilePickerTypes showFilePicker = FileOperations.FilePickerTypes.None)
        {
            const float leftPadding = 200;
            const float spacing = 20;
            var isFilePickerVisible = showFilePicker != FileOperations.FilePickerTypes.None;
            float spaceForFilePicker = isFilePickerVisible ? 30 : 0;
            var labelSize = ImGui.CalcTextSize(label);

            var p = ImGui.GetCursorPos();
            ImGui.SetCursorPosX( MathF.Max(leftPadding - labelSize.X,0)+10);
            ImGui.AlignTextToFramePadding();

            var cleanedLabel = label.Split(ImGuiIdSpecifier)[0];
            ImGui.TextUnformatted(cleanedLabel);

            ImGui.SetCursorPos(p);
            
            ImGui.SameLine();
            ImGui.SetCursorPosX(leftPadding + spacing);
            
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X- 50 - spaceForFilePicker);
            
            var wasNull = value == null;
            if (wasNull)
                value = string.Empty;
            
            var modified = ImGui.InputText("##" + label, ref value, 1000);
            if (!modified && wasNull)
                value = null;

            if (string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(placeHolder))
            {
                var drawList = ImGui.GetWindowDrawList();
                var minPos = ImGui.GetItemRectMin();
                var maxPos = ImGui.GetItemRectMax();
                drawList.PushClipRect(minPos,maxPos);
                drawList.AddText(minPos + new Vector2(8,3), Color.White.Fade(0.25f), placeHolder);
                drawList.PopClipRect();
            }

            
            if (isFilePickerVisible)
            {
                modified |= FileOperations.DrawFileSelector(showFilePicker, ref value);                
            } 

            if (!string.IsNullOrEmpty(warning))
            {
                ImGui.Indent(leftPadding + spacing);
                ImGui.PushFont(Fonts.FontSmall);
                ImGui.PushStyleColor(ImGuiCol.Text, Color.Red.Rgba);
                ImGui.TextUnformatted(warning);
                ImGui.PopStyleColor();
                ImGui.PopFont();
                ImGui.Unindent(leftPadding + spacing);
            }
            
            return modified;
        }

        public static bool DrawSearchField(string placeHolderLabel, ref string value, float width=0)
        {
            var wasNull = value == null;
            if (wasNull)
                value = string.Empty;
            
            ImGui.SetNextItemWidth(width-20);
            var modified = ImGui.InputText("##" + placeHolderLabel, ref value, 1000);
            if (!modified && wasNull)
                value = null;

            if (!string.IsNullOrEmpty(value))
            {
                ImGui.SameLine();
                if (ImGui.Button("×"))
                {
                    value = null;
                    modified = true;
                }
            }
            else
            {
                var drawList = ImGui.GetWindowDrawList();
                var minPos = ImGui.GetItemRectMin();
                var maxPos = ImGui.GetItemRectMax();
                drawList.PushClipRect(minPos, maxPos);
                drawList.AddText(minPos + new Vector2(8, 5), Color.White.Fade(0.25f), placeHolderLabel);
                drawList.PopClipRect();
            }

            return modified;
        }        
        
        
        public static bool DrawEnumParameter<T>(ref int index, string label)
        {
            // Label
            var labelSize = ImGui.CalcTextSize(label);

            var p = ImGui.GetCursorPos();
            ImGui.SetCursorPosX(MathF.Max(200 - labelSize.X, 0) + 10);
            ImGui.AlignTextToFramePadding();

            string cleanedLabel = label.Split(ImGuiIdSpecifier)[0];
            ImGui.TextUnformatted(cleanedLabel);

            ImGui.SetCursorPos(p);

            // Dropdown
            ImGui.SameLine();
            ImGui.SetCursorPosX(220);
            var size = new Vector2(150, ImGui.GetFrameHeight());
            
            Type enumType = typeof(T);
            var values = Enum.GetValues(enumType);

            var valueNames = new string[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                var v = values.GetValue(i);
                valueNames[i] = v != null
                                    ? Enum.GetName(typeof(T), v)
                                    : "?? undefined";
            }

            ImGui.SetNextItemWidth(size.X);
            // FIXME: using only "##dropdown" did not allow for multiple combos (see for example renderSequenceWindow.cs)
            // so we add the type and label here - but this is only a temporary hack...
            var modified = ImGui.Combo($"##dropDown{enumType}{label}", ref index, valueNames, valueNames.Length, valueNames.Length);
            return modified;
        }

        private const string ImGuiIdSpecifier = "##";
    }
}