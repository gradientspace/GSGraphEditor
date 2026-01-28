// Copyright Gradientspace Corp. All Rights Reserved.
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using GSNodeEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GSNodeEditor.EditorHostAPI;

namespace GraphEditorAppV2
{
    class GraphEditorHostImpl : EditorHostAPI
	{
		MainWindow AppMainWindow;

		public GraphEditorHostImpl(MainWindow topWindow)
		{
			AppMainWindow = topWindow;
		}


		public void RequestRepaint()
		{
			// make sure this runs on the UI thread
			Dispatcher.UIThread.InvokeAsync( () => { AppMainWindow.SkiaView.InvalidateVisual(); }, DispatcherPriority.Background);
			//	InvalidateVisual, DispatcherPriority.Render)

			// ignore for now - will get called from a background thread and we need to forward it to foreground thread?
			//MainWindow.InvalidateVisual();
		}

		public void SetWindowTitle(string NewTitle)
		{
			AppMainWindow.Title = NewTitle;
		}

        //! Filter should be formatted like so: "Text Files (*.txt)|*.txt|All files (*.*)|*.*";
        //! Extension should not not include a dot: "txt"
        public async Task<FileDialogResult> ShowSaveAsDialogAsync(
			string InitialFilename,
			string Extension,
			string Filter,
			string? InitialFolder)
		{
            FileDialogResult Result = new();
			TopLevel? topLevel = TopLevel.GetTopLevel(AppMainWindow) ?? throw new NullReferenceException();

			List<FilePickerFileType> FileTypes = new List<FilePickerFileType>() {
                new FilePickerFileType("Node Graphs") { Patterns = new[] { "*.gg" } },
                new FilePickerFileType("Node Graphs (old)") { Patterns = new[] { "*.json" } },
				new FilePickerFileType("All Files") {Patterns = new [] { "*.*" } }
			};

			IStorageFolder? initialFolder = null;
			if (InitialFolder != null)
			{
                initialFolder = await topLevel.StorageProvider.TryGetFolderFromPathAsync(InitialFolder);
			}

			IStorageFile? files = await topLevel.StorageProvider.SaveFilePickerAsync(
				new FilePickerSaveOptions() {
					DefaultExtension = Extension,
					FileTypeChoices = FileTypes,
					SuggestedFileName = InitialFilename,
					SuggestedStartLocation = initialFolder
				});

            if (files == null) {
                Result.bCanceled = true;
                return Result;
            }
			string? localPath = files.TryGetLocalPath();
            if (localPath == null)
                return Result;
            Result.SelectedPath = localPath;
            Result.bSuccess = true;
            return Result;
		}

        //! Filter should be formatted like so: "Text Files (*.txt)|*.txt|All files (*.*)|*.*";
        //! Extension should not not include a dot: "txt"
        public async Task<FileDialogResult> ShowOpenFileDialogAsync(
			string Extension,
			string Filter,
			string? InitialFilename,
			string? InitialFolder)
		{
            FileDialogResult Result = new(); 
            TopLevel? topLevel = TopLevel.GetTopLevel(AppMainWindow) ?? throw new NullReferenceException();

			List<FilePickerFileType> FileTypes = new List<FilePickerFileType>() {
                new FilePickerFileType("Node Graphs") { Patterns = new[] { "*.gg" } },
                new FilePickerFileType("Node Graphs (old)") { Patterns = new[] { "*.json" } },
				new FilePickerFileType("All Files") {Patterns = new [] { "*.*" } }
			};

			IStorageFolder? initialFolder = null;
			if (InitialFolder != null)
			{
				initialFolder = await topLevel.StorageProvider.TryGetFolderFromPathAsync(InitialFolder);
			}

			IReadOnlyList<IStorageFile> files = await topLevel.StorageProvider.OpenFilePickerAsync(
				new FilePickerOpenOptions() {
					FileTypeFilter = FileTypes,
					SuggestedFileName = InitialFilename,
					AllowMultiple = false,
					SuggestedStartLocation = initialFolder
				});

			if (files.Count == 0) {
                Result.bCanceled = true;
                return Result;
            }
			string? localPath = files[0].TryGetLocalPath();
			if (localPath == null) 
                return Result;
            Result.SelectedPath = localPath;
            Result.bSuccess = true;
            return Result;
		}


        public virtual async void SetSystemClipboardText(string NewText)
        {
            TopLevel? topLevel = TopLevel.GetTopLevel(AppMainWindow);
            if (topLevel != null && topLevel.Clipboard != null)
                await topLevel.Clipboard.SetTextAsync(NewText);
        }

        public virtual string? GetSystemClipboardText()
        {
            TopLevel? topLevel = TopLevel.GetTopLevel(AppMainWindow);
            if (topLevel != null && topLevel.Clipboard != null)
                return topLevel.Clipboard.GetTextAsync().Result;
            return null;
        }

    }
}
