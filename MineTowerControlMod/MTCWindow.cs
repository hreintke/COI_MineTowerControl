using Mafi.Unity.UserInterface;
using Mafi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mafi.Unity.UiFramework.Components;
using Mafi.Unity.UiFramework;
using UnityEngine;
using Mafi.Core.Syncers;
using Mafi.Core.Products;
using Mafi.Unity.InputControl.Inspectors;
using Mafi.Unity;
using Mafi.Collections.ImmutableCollections;
using Mafi.Core.Prototypes;
using Mafi.Core.Entities;
using Mafi.Core.Buildings.Settlements;
using Mafi.Core.Buildings.Mine;
using Mafi.Collections;
using Mafi.Unity.UserInterface.Components;
using static System.Collections.Specialized.BitVector32;
using Mafi.Unity.Camera;
using Mafi.Core.Input;
using Mafi.Unity.InputControl.RecipesBook;
using Mafi.Unity.InputControl;

namespace MineTowerControlMod
{
    [GlobalDependency(RegistrationMode.AsEverything)]
    public class MTCWindow : WindowView, IWindowWithInnerWindowsSupport
    {
        private StackContainer mineTowerStack;
        private Txt countLabel;
        private EntitiesManager _entitiesManager;
        private CameraController _cameraController;
        private InputScheduler  _inputScheduler;
        private ProtosFilterEditor<ProductProto> productFilterView;
        private MTCController _mtcController;
        ImmutableArray<ProductProto> _allDumpableProducts;
        private IUiUpdater mineTowerUIUpdater;

        ScrollableStackContainer mainScroll;

        public MTCWindow( ProtosDb protosDb,
                            EntitiesManager entitiesManager,
                            CameraController cameraController,
                            InputScheduler inputScheduler) : base("MTCWindow")

        {
             _entitiesManager = entitiesManager;
            _cameraController = cameraController;
            _inputScheduler = inputScheduler;
            _allDumpableProducts = protosDb.All<LooseProductProto>().Where<LooseProductProto>((Func<LooseProductProto, bool>)(x => x.CanBeOnTerrain)).Cast<ProductProto>().ToImmutableArray<ProductProto>();
        }

        protected override void BuildWindowContent()
        {
            LogWrite.Info("Building MTC Window");
            SetTitle("MineTower Control");
            SetContentSize(500f, 600f);
            PositionSelfToCenter();
            MakeMovable();
      


           mineTowerStack = Builder
              .NewStackContainer("mts")
                .SetStackingDirection(StackContainer.Direction.TopToBottom)
                .SetSizeMode(StackContainer.SizeMode.Dynamic)
                .SetItemSpacing(30f);

            mainScroll = new ScrollableStackContainer(Builder, 600, mineTowerStack);
            mineTowerUIUpdater = UpdaterBuilder.Start().Build();

            this.AddUpdater(mineTowerUIUpdater);

            mainScroll.PutTo(GetContentPanel());
        }

        public void refreshContent()
        {
            LogWrite.Info($"Refreshing MTC Content {_entitiesManager.GetCountOf<MineTower>(t => true)}");

            mainScroll.ItemsContainer.StartBatchOperation();
             mainScroll.ItemsContainer.ClearAll(true);
           // mainScroll.ItemsContainer.ClearAndDestroyAll();

            mineTowerUIUpdater.ClearAllChildUpdaters();
            UpdaterBuilder updaterBuilder = UpdaterBuilder.Start();

            //First item is the Button & Selection

            StackContainer buttonSelection =
                Builder
                    .NewStackContainer("mti")
                    .SetStackingDirection(StackContainer.Direction.LeftToRight)
                    .SetSizeMode(StackContainer.SizeMode.Dynamic)
                    .SetItemSpacing(10f);

            Btn clearButton = 
                Builder.NewBtnGeneral("Hide", buttonSelection)
                    .SetButtonStyle(Style.Global.GeneralBtn)
                    .SetText("Clear Finished")
                    .OnClick(() => { _mtcController.clearFinishedDesignations(); })
                    .SetOnMouseEnterLeaveActions(() => { _mtcController.showAreas(); }, () => { _mtcController.hideAreas(); })
                    .AppendTo(buttonSelection, new Vector2(60, 30), ContainerPosition.MiddleOrCenter, new Offset(0,0,10,0));
            
            mainScroll.ItemsContainer.Append(buttonSelection, new Vector2(100f, 30f), ContainerPosition.LeftOrTop, new Offset(0, 10, 10, 10));
            mainScroll.ItemsContainer.AppendDivider(2f, ColorRgba.Gray);



            foreach (MineTower m in _entitiesManager.GetAllEntitiesOfType<MineTower>().OrderBy(mt => mt.CustomTitle.HasValue ? mt.CustomTitle.Value : "z" + mt.Transform.Position.Xy.ToString()))
            {
                StackContainer mineTowerInstance =
                  Builder
                     .NewStackContainer("mti")
                     .SetStackingDirection(StackContainer.Direction.LeftToRight)
                     .SetSizeMode(StackContainer.SizeMode.Dynamic)
                     .SetItemSpacing(10f);

                StackContainer textButtonContainer =
                    Builder
                      .NewStackContainer("tbc")
                      .SetStackingDirection(StackContainer.Direction.TopToBottom)
                      .SetSizeMode(StackContainer.SizeMode.Dynamic)
                      .SetItemSpacing(10f);

                Txt mineTowerName =
                    Builder.NewTxt("XL")
                      .SetHeight(25)
                      .SetTextStyle(Builder.Style.Global.TextControls)
                      .SetText("Tower Name")
                      .AppendTo(textButtonContainer, new Vector2(100, 15), ContainerPosition.MiddleOrCenter, new Offset(0, 0, 5, 0));

                updaterBuilder.Observe<Option<string>>(() => m.CustomTitle).Do((s) => { mineTowerName.SetText(m.CustomTitle.HasValue ? m.CustomTitle.Value : m.Transform.Position.Xy.ToString()); });

                StackContainer buttonContainer =
                  Builder
                    .NewStackContainer("bc")
                    .SetStackingDirection(StackContainer.Direction.LeftToRight)
                    .SetSizeMode(StackContainer.SizeMode.Dynamic)
                    .SetItemSpacing(10f);

                Btn s = Builder.NewBtnGeneral("Show", buttonContainer)
                .SetButtonStyle(Style.Global.GeneralBtn)
                .SetIcon(Builder.Style.Icons.Show, Offset.All(1f))
                .OnClick(() => { _cameraController.PanTo(new Tile2f(m.Transform.Position.X, m.Transform.Position.Y)); })
                .SetOnMouseEnterLeaveActions(() => { _mtcController.setActiveTower(m); }, () => { _mtcController.resetActiveTower(); })
                .AppendTo(buttonContainer, new Vector2(40, 25), ContainerPosition.MiddleOrCenter, Offset.Top(10f));

                Btn b = Builder.NewBtnGeneral("state", buttonContainer)
                    .SetButtonStyle(Style.Global.GeneralBtn)
                    .SetIcon(Builder.Style.Icons.Pause, Offset.All(1f))
                    .OnClick(() => { m.SetPaused(m.IsPaused ? false : true); })
                    .AppendTo(buttonContainer, new Vector2(40, 25), ContainerPosition.MiddleOrCenter, Offset.Top(10f))
                    .SetBackgroundColor(m.IsPaused ? ColorRgba.Red : ColorRgba.Green);

                updaterBuilder.Observe<bool>(() => { return m.IsPaused; }).Do((Action<bool>)(mp => { b.SetBackgroundColor(mp ? ColorRgba.Red : ColorRgba.Green); }));

                textButtonContainer.Append(buttonContainer, new Vector2(50f, 30f), ContainerPosition.LeftOrTop);

                mineTowerInstance.Append(textButtonContainer, new Vector2(100f, 40f), ContainerPosition.LeftOrTop);

                productFilterView = new ProtosFilterEditor<ProductProto>(
                this.Builder,
                (IWindowWithInnerWindowsSupport)this,
                mineTowerInstance,
                new Action<ProductProto>((p) => { RemoveDumpableProduct(m, p); }),
                new Action<ProductProto>((p) => { AddDumpableProduct(m, p); }),
                new Func<IEnumerable<ProductProto>>(_allDumpableProducts.AsEnumerable),
                (Func<IEnumerable<ProductProto>>)(() => (IEnumerable<ProductProto>)m.DumpableProducts),
                columnsCount: 4,
                usePrimaryBtnStyle: false);
                updaterBuilder.Observe<ProductProto>((Func<IReadOnlyCollection<ProductProto>>)(() => (IReadOnlyCollection<ProductProto>)m.DumpableProducts), (ICollectionComparator<ProductProto, IReadOnlyCollection<ProductProto>>)CompareByCount<ProductProto>.Instance).Do(new Action<Lyst<ProductProto>>(productFilterView.UpdateFilteredProtos));
                //              productFilterView.SetTextToShowWhenEmpty(string.Format("({0})", (object)Tr.DumpingFilter__Empty));
                mainScroll.ItemsContainer.Append(mineTowerInstance, new Vector2(200f, 40f), ContainerPosition.LeftOrTop, new Offset(0, 0, 10, 5));
                mainScroll.ItemsContainer.AppendDivider(2f, ColorRgba.Gray);
                mainScroll.ItemsContainer.FinishBatchOperation();
            }
            mineTowerUIUpdater.AddChildUpdater(updaterBuilder.Build());
        }


        public void SetupInnerWindowWithButton(
          WindowView innerWindow,
          IUiElement btnHolder,
          IUiElement btn,
          Action returnBtnHolder,
          Action onExitAction)
        {
            innerWindow.BuildUi(this.Builder);
 //           innerWindow.MakeMovable((Action<Offset>)(o => ItemDetailWindowView.s_windowOffsets[(int)this.m_windowOffsetGroup] = o), (IUiElement)this);
              innerWindow.PutToLeftTopOf<WindowView>(btnHolder, innerWindow.GetSize(), Offset.Left(btn.GetWidth() - 1f));
            //          Panel overlay = this.AddOverlay(onExitAction);
            LogWrite.Info("InnerWindow Instantiate");
           innerWindow.OnShowStart += (Action)(() =>
            {
                LogWrite.Info("InnerWindow ShowStart");
                //              overlay.Show<Panel>();
                btnHolder.SetParent<IUiElement>((IUiElement)this);

            });
            innerWindow.OnHide += (Action)(() =>
            {
                LogWrite.Info($"InnerWindow Hide {returnBtnHolder.ToString()} {returnBtnHolder == null}");
                //            overlay.Hide<Panel>();
                returnBtnHolder();
            });
            
           
        }

        public void AddDumpableProduct(MineTower tower, ProductProto product)
        {
            _inputScheduler.ScheduleInputCmd<AddProductToDumpCmd>(new AddProductToDumpCmd((Option<MineTower>)tower, product));
        }

        public void RemoveDumpableProduct(MineTower tower, ProductProto product)
        {
            _inputScheduler.ScheduleInputCmd<RemoveProductToDumpCmd>(new RemoveProductToDumpCmd((Option<MineTower>)tower, product));
        }

        internal void SetController(MTCController mtcController)
        {
            _mtcController = mtcController;
        }
    }
}
