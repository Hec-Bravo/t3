﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using T3.Editor.Gui.Graph;
using ImGuiNET;
using T3.Core.Animation;
using T3.Core.Audio;
using T3.Core.IO;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Editor.Gui.Audio;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Dialog;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Graph.Rendering;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.Interaction.Timing;
using T3.Editor.Gui.Interaction.Variations;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.Templates;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.UiHelpers.Wiki;
using T3.Editor.Gui.Windows;
using T3.Editor.Gui.Windows.Layouts;
using T3.Operators.Types.Id_5d7d61ae_0a41_4ffa_a51d_93bab665e7fe;
using T3.Operators.Types.Id_79db48d8_38d3_47ca_9c9b_85dde2fa660d;
using T3.Operators.Types.Id_cda108a1_db4f_4a0a_ae4d_d50e9aade467;

namespace T3.Editor.Gui
{
    public class T3Ui
    {
        static T3Ui()
        {
            var operatorsAssembly = Assembly.GetAssembly(typeof(Value));
            UiModel = new UiModel(operatorsAssembly);

            var playback = new Playback();
            //WindowManager = new WindowManager();
            WindowManager.TryToInitialize();
            ExampleSymbolLinking.UpdateExampleLinks();
            VariationHandling.Init();

        }

        private void InitializeAfterAppWindowReady()
        {
            if (_initialed || ImGui.GetWindowSize() == Vector2.Zero)
                return;
            
            _initialed = true;
        }

        private bool _initialed = false;

        public void Draw()
        {
            //InitializeAfterAppWindowReady();
            
            // Prepare the current frame 
            Playback.Current.Update(UserSettings.Config.EnableIdleMotion);
            SoundtrackUtils.UpdateMainSoundtrack();
            BeatTiming.Update(ImGui.GetTime());
            _bpmDetection.AddFftSample(AudioAnalysis.FftGainBuffer);
            
            AudioEngine.CompleteFrame(Playback.Current);    // Update
            
            if (ForwardBeatTaps.BeatTapTriggered)
                BeatTiming.TriggerSyncTap();

            if (ForwardBeatTaps.ResyncTriggered)
                BeatTiming.TriggerResyncMeasure();

            AutoBackup.AutoBackup.IsEnabled = UserSettings.Config.EnableAutoBackup;
            OpenedPopUpName = string.Empty;

            VariationHandling.Update();
            MouseWheelFieldWasHoveredLastFrame = MouseWheelFieldHovered;
            MouseWheelFieldHovered = false;

            FitViewToSelectionHandling.ProcessNewFrame();
            SrvManager.FreeUnusedTextures();
            KeyboardBinding.InitFrame();
            
            // Set selected id so operator can check if they are selected or not  
            var selectedInstance = NodeSelection.GetSelectedInstance();
            MouseInput.SelectedChildId = selectedInstance?.SymbolChildId ?? Guid.Empty; 
            
            // Draw everything!
            ImGui.DockSpaceOverViewport();
            WindowManager.Draw();
            
            // Complete frame
            SingleValueEdit.StartNextFrame();
            SelectableNodeMovement.CompleteFrame();

            FrameStats.CompleteFrame();
            TriggerGlobalActionsFromKeyBindings();
            
            if ( UserSettings.Config.ShowMainMenu || ImGui.GetMousePos().Y < 20)
            {
                DrawAppMenu();
            }
            
            _userNameDialog.Draw();
            _importDialog.Draw();
            _createFromTemplateDialog.Draw();
            
            if (!UserSettings.IsUserNameDefined() )
            {
                UserSettings.Config.UserName = Environment.UserName;
                _userNameDialog.ShowNextFrame();
            }            
            
            AutoBackup.AutoBackup.CheckForSave();
        }
        


        private void TriggerGlobalActionsFromKeyBindings()
        {
            if (KeyboardBinding.Triggered(UserActions.Undo))
            {
                UndoRedoStack.Undo();
            }
            else if (KeyboardBinding.Triggered(UserActions.Redo))
            {
                UndoRedoStack.Redo();
            }
            else if (KeyboardBinding.Triggered(UserActions.Save))
            {
                SaveInBackground(saveAll:true);
            }
            else if (KeyboardBinding.Triggered(UserActions.ToggleFocusMode))
            {
                ToggleFocusMode();
            }
        }
        
        private void DrawAppMenu()
        {
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6, 6));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(6, 6));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
            
            if (ImGui.BeginMainMenuBar())
            {
                ImGui.SetCursorPos(new Vector2(0,-1)); // Shift to make menu items selected when hitting top of screen
                
                if (ImGui.BeginMenu("File"))
                {
                    UserSettings.Config.ShowMainMenu = true;

                    if (ImGui.MenuItem("New...", KeyboardBinding.ListKeyboardShortcuts(UserActions.New, false), false, !IsCurrentlySaving))
                    {
                        _createFromTemplateDialog.ShowNextFrame();
                    }
                    
                    if (ImGui.MenuItem("Import Operators", null, false, !IsCurrentlySaving))
                    {
                        _importDialog.ShowNextFrame();
                    }

                    if (ImGui.MenuItem("Save", KeyboardBinding.ListKeyboardShortcuts(UserActions.Save, false), false, !IsCurrentlySaving))
                    {
                        SaveInBackground(saveAll:true);
                    }

                    if (ImGui.MenuItem("Quit", !IsCurrentlySaving))
                    {
                        Application.Exit();
                    }

                    if (ImGui.IsItemHovered() && IsCurrentlySaving)
                    {
                        ImGui.SetTooltip("Can't exit while saving is in progress");
                    }
                    
                    ImGui.Separator();
                    if (ImGui.BeginMenu("Documentation"))
                    {
                        if (ImGui.MenuItem("Export Operator Descriptions"))
                        {
                            ExportWikiDocumentation.ExportWiki();
                        }
                        ImGui.EndMenu();
                    }
                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Edit"))
                {
                    UserSettings.Config.ShowMainMenu = true;
                    if (ImGui.MenuItem("Undo", "CTRL+Z", false, UndoRedoStack.CanUndo))
                    {
                        UndoRedoStack.Undo();
                    }

                    if (ImGui.MenuItem("Redo", "CTRL+Y", false, UndoRedoStack.CanRedo))
                    {
                        UndoRedoStack.Redo();
                    }

                    ImGui.Separator();

                    if (ImGui.MenuItem("Fix File references", ""))
                    {
                        FileReferenceOperations.FixOperatorFilepathsCommand_Executed();
                    }

                    if (ImGui.BeginMenu("Bookmarks"))
                    {
                        GraphBookmarkNavigation.DrawBookmarksMenu();
                        ImGui.EndMenu();
                    }

                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Add"))
                {
                    UserSettings.Config.ShowMainMenu = true;
                    SymbolTreeMenu.Draw();
                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("View"))
                {
                    UserSettings.Config.ShowMainMenu = true;
                    

                    ImGui.Separator();
                    ImGui.MenuItem("Show Main Menu", "", ref UserSettings.Config.ShowMainMenu);
                    ImGui.MenuItem("Show Title", "", ref UserSettings.Config.ShowTitleAndDescription);
                    ImGui.MenuItem("Show Timeline", "", ref UserSettings.Config.ShowTimeline);
                    ImGui.MenuItem("Show Minimap", "", ref UserSettings.Config.ShowMiniMap);
                    ImGui.MenuItem("Show Toolbar", "", ref UserSettings.Config.ShowToolbar);
                    if(ImGui.MenuItem("Toggle All UI Elements", KeyboardBinding.ListKeyboardShortcuts(UserActions.ToggleFocusMode, false), false, !IsCurrentlySaving))
                    {
                        ToggleFocusMode();
                    }
                    
                    ImGui.Separator();
                    ImGui.MenuItem("FullScreen", "", ref UserSettings.Config.FullScreen);
                    ImGui.EndMenu();
                }
                
                if (ImGui.BeginMenu("Windows"))
                {
                    WindowManager.DrawWindowMenuContent();
                    ImGui.EndMenu();
                }


                T3Metrics.DrawRenderPerformanceGraph();
                _statusErrorLine.Draw();

                ImGui.EndMainMenuBar();
            }

            ImGui.PopStyleVar(3);
        }


        private void ToggleFocusMode()
        {
                //T3Ui.MaximalView = !T3Ui.MaximalView;
                if (UserSettings.Config.ShowToolbar)
                {
                    UserSettings.Config.ShowMainMenu = false;
                    UserSettings.Config.ShowTitleAndDescription = false;
                    UserSettings.Config.ShowToolbar = false;
                    UserSettings.Config.ShowTimeline = false;
                }
                else
                {
                    UserSettings.Config.ShowMainMenu = true;
                    UserSettings.Config.ShowTitleAndDescription = true;
                    UserSettings.Config.ShowToolbar = true;
                    UserSettings.Config.ShowTimeline = true;
                }
            
        }
        
        private static readonly object _saveLocker = new object();
        private static readonly Stopwatch _saveStopwatch = new Stopwatch();

        private static void SaveInBackground(bool saveAll)
        {
            if (saveAll)
            {
                Task.Run(SaveAll);
            }
            else
            {
                Task.Run(SaveModified);
            }
        }

        public static void SaveModified()
        {
            lock (_saveLocker)
            {
                if (_saveStopwatch.IsRunning)
                {
                    Log.Debug("Can't save modified while saving is in progress");
                    return;
                }                
                _saveStopwatch.Restart();
                UiModel.SaveModifiedSymbols();
                _saveStopwatch.Stop();
                Log.Debug($"Saving took {_saveStopwatch.ElapsedMilliseconds}ms.");
            }
        }

        public static void SaveAll()
        {
            lock (_saveLocker)
            {
                if (_saveStopwatch.IsRunning)
                {
                    Log.Debug("Can't save while saving is in progress");
                    return;
                }
                
                _saveStopwatch.Restart();
                UiModel.SaveAll();
                _saveStopwatch.Stop();
                Log.Debug($"Saving took {_saveStopwatch.ElapsedMilliseconds}ms.");
            }
        }



        public static void SelectAndCenterChildIdInView(Guid symbolChildId)
        {
            var primaryGraphWindow = GraphWindow.GetPrimaryGraphWindow();
            if (primaryGraphWindow == null)
                return;

            var compositionOp = primaryGraphWindow.GraphCanvas.CompositionOp;

            var symbolUi = SymbolUiRegistry.Entries[compositionOp.Symbol.Id];
            var sourceSymbolChildUi = symbolUi.ChildUis.SingleOrDefault(childUi => childUi.Id == symbolChildId);
            var selectionTargetInstance = compositionOp.Children.Single(instance => instance.SymbolChildId == symbolChildId);
            NodeSelection.SetSelectionToChildUi(sourceSymbolChildUi, selectionTargetInstance);
            FitViewToSelectionHandling.FitViewToSelection();
        }

        // private static void SwapHoveringBuffers()
        // {
        //     (HoveredIdsLastFrame, _hoveredIdsForNextFrame) = (_hoveredIdsForNextFrame, HoveredIdsLastFrame);
        //     _hoveredIdsForNextFrame.Clear();
        //     
        //     (RenderedIdsLastFrame, _renderedIdsForNextFrame) = (_renderedIdsForNextFrame, RenderedIdsLastFrame);
        //     _renderedIdsForNextFrame.Clear();            
        // }
        
        /// <summary>
        /// Statistics method for debug purpose
        /// </summary>
        private static void CountSymbolUsage()
        {
            var counts = new Dictionary<Symbol, int>();
            foreach (var s in SymbolRegistry.Entries.Values)
            {
                foreach (var child in s.Children)
                {
                    if (!counts.ContainsKey(child.Symbol))
                        counts[child.Symbol] = 0;
                    
                    counts[child.Symbol]++;
                }
            }
            foreach(var (s,c) in counts.OrderBy(c => counts[c.Key]).Reverse())
            {
                Log.Debug($"{s.Name} - {s.Namespace}  {c}");
            }
        }

        private readonly StatusErrorLine _statusErrorLine = new();
        public static readonly UiModel UiModel;

        public static string OpenedPopUpName; // This is reset on Frame start and can be useful for allow context menu to stay open even if a
        // later context menu would also be opened. There is probably some ImGui magic to do this probably. 

        public static IntPtr NotDroppingPointer = new IntPtr(0);
        public static bool DraggingIsInProgress = false;
        public static bool MouseWheelFieldHovered { private get; set; }
        public static bool MouseWheelFieldWasHoveredLastFrame { get; private set; }
        public static bool ShowSecondaryRenderWindow => WindowManager.ShowSecondaryRenderWindow;
        public const string FloatNumberFormat = "{0:F2}";
        public static bool IsCurrentlySaving => _saveStopwatch != null && _saveStopwatch.IsRunning;

        //private static readonly AutoBackup.AutoBackup _autoBackup = new();
        
        private static readonly CreateFromTemplateDialog _createFromTemplateDialog = new();
        private static readonly UserNameDialog _userNameDialog = new();
        private static readonly MigrateOperatorsDialog _importDialog = new();

        public static readonly BpmDetection _bpmDetection = new ();

        [Flags]
        public enum EditingFlags
        {
            None = 0,
            ExpandVertically = 1 << 1,
            PreventMouseInteractions = 1 << 2,
            PreventZoomWithMouseWheel = 1 << 3,
            PreventPanningWithMouse = 1 << 4,
        }

        public static bool UseVSync = true;
        public static bool ItemRegionsVisible;
    }
}