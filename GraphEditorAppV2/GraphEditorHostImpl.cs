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
		public bool ShowBlockingSaveAsDialog(
			string InitialFilename,
			string Extension,
			string Filter,
			string? InitialFolder,
			out string SelectedFilename)
		{
			SelectedFilename = "";
			TopLevel? topLevel = TopLevel.GetTopLevel(AppMainWindow) ?? throw new NullReferenceException();

			List<FilePickerFileType> FileTypes = new List<FilePickerFileType>() {
				new FilePickerFileType("Node Graphs") { Patterns = new[] { "*.json" } },
				new FilePickerFileType("All Files") {Patterns = new [] { "*.*" } }
			};

			IStorageFolder? initialFolder = null;
			if (InitialFolder != null)
			{
				Task<IStorageFolder?> foundFolder = topLevel.StorageProvider.TryGetFolderFromPathAsync(InitialFolder);
				foundFolder.Wait();
				initialFolder = foundFolder.Result;
			}

			Task<IStorageFile?> files = topLevel.StorageProvider.SaveFilePickerAsync(
				new FilePickerSaveOptions() {
					DefaultExtension = Extension,
					FileTypeChoices = FileTypes,
					SuggestedFileName = InitialFilename,
					SuggestedStartLocation = initialFolder
				});
			files.Wait();

			if (files.Result == null)
				return false;
			string? localPath = files.Result.TryGetLocalPath();
			if (localPath == null)
				return false;
			SelectedFilename = localPath;
			return true;
		}

		//! Filter should be formatted like so: "Text Files (*.txt)|*.txt|All files (*.*)|*.*";
		//! Extension should not not include a dot: "txt"
		public bool ShowBlockingOpenFileDialog(
			string Extension,
			string Filter,
			string? InitialFilename,
			string? InitialFolder,
			out string SelectedFilename)
		{
			SelectedFilename = "";
			TopLevel? topLevel = TopLevel.GetTopLevel(AppMainWindow) ?? throw new NullReferenceException();

			List<FilePickerFileType> FileTypes = new List<FilePickerFileType>() {
				new FilePickerFileType("Node Graphs") { Patterns = new[] { "*.json" } },
				new FilePickerFileType("All Files") {Patterns = new [] { "*.*" } }
			};

			IStorageFolder? initialFolder = null;
			if (InitialFolder != null)
			{
				Task<IStorageFolder?> foundFolder = topLevel.StorageProvider.TryGetFolderFromPathAsync(InitialFolder);
				foundFolder.Wait();
				initialFolder = foundFolder.Result;
			}

			Task<IReadOnlyList<IStorageFile>> files = topLevel.StorageProvider.OpenFilePickerAsync(
				new FilePickerOpenOptions() {
					FileTypeFilter = FileTypes,
					SuggestedFileName = InitialFilename,
					AllowMultiple = false,
					SuggestedStartLocation = initialFolder
				});
			files.Wait();

			if (files.Result.Count == 0)
				return false;
			string? localPath = files.Result[0].TryGetLocalPath();
			if (localPath == null)
				return false;
			SelectedFilename = localPath;
			return true;
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
