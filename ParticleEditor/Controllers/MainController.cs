﻿using System;
using System.Diagnostics;

using Gtk;

using MG.EditorCommon;
using MG.EditorCommon.Undo;
using MG.Framework.Assets;
using MG.Framework.Particle;
using MG.Framework.Utility;
using MG.ParticleEditorWindow;

namespace MG.ParticleEditor.Controllers
{
	class MainController : IDisposable
	{
		private MainWindow window;
		private Model model;
		private AssetHandler assetHandler;
		private DocumentController documentController;
		private RenderController renderController;
		private TreeController treeController;
		private PropertyController propertyController;

		private Stopwatch startStopwatch = new Stopwatch();
		private Stopwatch frameStopwatch = new Stopwatch();
		
		public MainController()
		{
			window = new MainWindow("Window");
			window.Closing += WindowOnClosing;
			window.Closed += WindowOnClosed;

			model = new Model();
			model.UndoHandler = new UndoHandler(1000);
			model.UndoHandler.AfterStateChanged += AfterUndo;
			model.Declaration = new ParticleDeclarationTable();
			model.Declaration.Load("ParticleDeclarations.xml");
			
			model.Definition = new ParticleDefinitionTable();
			//model.Definition.Load("definitions.xml");
			
			assetHandler = new AssetHandler(".");

			documentController = new DocumentController(model, window);
			renderController = new RenderController(model, assetHandler, window.RenderView);
			treeController = new TreeController(model, window.TreeView);
			propertyController = new PropertyController(model, window.PropertyView);
			
			treeController.ItemSelected += renderController.OnItemSelected;
			treeController.ItemSelected += propertyController.OnChangeDefinition;
			documentController.NewDocument += treeController.OnNewDocument;
			documentController.New();

			AfterUndo();

			Application.Update += Update;
			startStopwatch.Start();
		}
		
		private void Update()
		{
			float elapsedSeconds = 0;
			if (!frameStopwatch.IsRunning)
			{
				frameStopwatch.Start();
			}
			else
			{
				elapsedSeconds = (float)frameStopwatch.Elapsed.TotalSeconds;
				frameStopwatch.Restart();
			}

			assetHandler.Update();
			renderController.Update(new Time(elapsedSeconds, startStopwatch.Elapsed.TotalSeconds));

			if (model.UpdateTree)
			{
				model.UpdateTree = false;
				treeController.UpdateTree();
			}

			window.StatusText = model.StatusText;
			window.RenderView.Refresh();
		}

		private void WindowOnClosed()
		{
			Application.Quit();
		}

		private void WindowOnClosing(MainWindow.ClosingEventArgs closingEventArgs)
		{
			closingEventArgs.Cancel = false;
		}

		private void AfterUndo()
		{
			window.UndoEnabled = model.UndoHandler.UndoSteps > 0;
			window.RedoEnabled = model.UndoHandler.RedoSteps > 0;
		}

		public void Dispose()
		{
			window.Dispose();
		}
	}
}