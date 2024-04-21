using Mafi.Unity.UiFramework;
using Mafi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mafi.Core.GameLoop;
using Mafi.Unity.InputControl;
using Mafi.Unity;
using Mafi.Core;
using UnityEngine;
using Mafi.Core.Input;
using Mafi.Unity.Mine;
using Mafi.Unity.Utils;
using Mafi.Core.Buildings.Towers;
using Mafi.Core.Buildings.Mine;
using Mafi.Unity.Entities;
using Mafi.Core.Buildings.Farms;
using Mafi.Unity.InputControl.Inspectors.Vehicles;
using Mafi.Unity.UserInterface;
using Mafi.Core.Prototypes;
using Mafi.Core.Entities;
using Mafi.Unity.Camera;
using Mafi.Collections;
using Mafi.Core.Terrain.Designation;

namespace MineTowerControlMod
{
    [GlobalDependency(RegistrationMode.AsEverything)]
    public class MTCController : BaseWindowController<MTCWindow>
    {
        private IUnityInputMgr _unityInputManager;
        private MTCWindow mtcWindow;
        private readonly KeyBindings WindowKey = KeyBindings.FromKey(KbCategory.General, ShortcutMode.Game, KeyCode.F10);
        private bool windowOpen = false;
        private ShortcutsManager shortcutsManager;
        private readonly Mafi.NewInstanceOf<EntityHighlighter> _entityHighlighter;
        private LinesFactory _linesFactory;
        public LineOverlayRendererHelper GoalLineRenderer;
        private readonly TowerAreasRenderer _towerAreasRenderer;
        private readonly IActivator _towerAreasAndDesignatorsActivator;
        private readonly InputScheduler _inputScheduler;
        private readonly TerrainMiningManager _terrainMiningManager;
        private readonly TerrainDumpingManager _terrainDumpingManager;
        private LineMb line;

        public MTCController(IUnityInputMgr unityInputManager,
            IGameLoopEvents gameLoop, 
            ShortcutsManager shortcutsManager,
            TowerAreasRenderer towerAreasRenderer,
            MTCWindow window,
            Mafi.NewInstanceOf<EntityHighlighter> entityHighlighter,
            LinesFactory linesFactory,
            UiBuilder builder,
            InputScheduler inputScheduler,
            TerrainMiningManager terrainMiningManager,
            TerrainDumpingManager terrainDumpingManager
            )
           : base(unityInputManager, gameLoop, builder, window)
        {
            mtcWindow = window;
            mtcWindow.SetController(this);
            _unityInputManager = unityInputManager;
            _entityHighlighter = entityHighlighter;
            _inputScheduler = inputScheduler;
            _terrainDumpingManager = terrainDumpingManager;
            _terrainMiningManager = terrainMiningManager;
            _linesFactory = linesFactory;
            _towerAreasRenderer = towerAreasRenderer;
            _towerAreasAndDesignatorsActivator = towerAreasRenderer.CreateCombinedActivatorWithTerrainDesignatorsAndGrid();

            this.GoalLineRenderer = new LineOverlayRendererHelper(linesFactory);
            this.GoalLineRenderer.SetWidth(1f);
            this.GoalLineRenderer.SetColor(Color.white);
            this.GoalLineRenderer.HideLine();

            unityInputManager.RegisterGlobalShortcut((Func<ShortcutsManager, KeyBindings>)(m => { return WindowKey; }), this);

        }

        public override void Activate()
        {
            LogWrite.Info("MTC Controller activated");
            windowOpen = true;

            base.Activate();
            mtcWindow.refreshContent();
            mtcWindow.Show();

            _towerAreasAndDesignatorsActivator.Activate();
        }

        public override void Deactivate()
        {
            LogWrite.Info("MTC Controller deactivated");
            windowOpen = false;
            _towerAreasAndDesignatorsActivator.Deactivate();
            _towerAreasRenderer.SelectTowerArea((Option<IAreaManagingTower>)Option.None);
            base.Deactivate();
        }

        public void setActiveTower(MineTower mineTower)
        {
            _towerAreasRenderer.SelectTowerArea(Option<IAreaManagingTower>.Some(mineTower));
            _entityHighlighter.Instance.Highlight(mineTower, ColorRgba.White);
            this.GoalLineRenderer.SetColor(mineTower.IsPaused ?  Color.red : Color.green);
            this.GoalLineRenderer.ShowLine(mineTower.Position3f, new Tile3f(mineTower.Area.CenterCoordF, 0)); 
        }

        public void resetActiveTower()
        {
            _towerAreasRenderer.SelectTowerArea((Option<IAreaManagingTower>)Option.None);
            _entityHighlighter.Instance.ClearAllHighlights();
            this.GoalLineRenderer.HideLine();
        }

        public void showAreas()
        {
            _towerAreasAndDesignatorsActivator.Activate();
        }

        public void hideAreas()
        {
            _towerAreasAndDesignatorsActivator.Deactivate();
        }

        public void clearFinishedDesignations()
        {
            Lyst<Tile2i> dl = new Lyst<Tile2i>();

            var dumpingTerrainDesignations = _terrainDumpingManager.DumpingDesignations
                .Where(x => x.IsFulfilled);
            foreach (var designation in dumpingTerrainDesignations)
            {
                dl.Add(designation.OriginTileCoord);
            }

            var miningTerrainDesignations = _terrainMiningManager.MiningDesignations
                .Where(x => x.IsFulfilled);
            foreach (var designation in miningTerrainDesignations)
            {
                dl.Add(designation.OriginTileCoord);
            }

            _inputScheduler.ScheduleInputCmd(new RemoveDesignationsCmd(dl.ToImmutableArray()));
        }
    }
}
