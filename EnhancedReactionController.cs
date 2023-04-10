using System;
using System.Collections.Generic;
using System.Text;
namespace EnhancedReactionMachine
{
    public class EnhancedReactionController : IController
    {
        // Settings for the game times
        private const int MAX_READY_TIME = 1000; //10s in ticks (Max Time for ready without pressing Go/Stop)          
        private const int MIN_WAIT_TIME = 100; //1s in ticks
        private const int MAX_WAIT_TIME = 250; //2.5s in ticks

        private const int MAX_GAME_TIME = 200; //2s in ticks(Max Game Time)

        private const int GAMEOVER_TIME = 300; //3s in ticks

        private const int RESULTS_TIME = 500; //5s in ticks(Time for displaying average)

        private const double TICKS_PER_SECOND = 100.0;

        private State _state;
        private IGui Gui { get; set; }
        private IRandom Rng { get; set; }
        private int Ticks { get; set; }
        private int Games { get; set; }
        private int TotalReactionTime { get; set; }
        public void Connect(IGui gui, IRandom rng)
        {
            Gui = gui;
            Rng = rng;
            Init();
        }

        public void Init() => _state = new State_On(this);

        public void CoinInserted() => _state.CoinInserted();
        public void GoStopPressed() => _state.GoStopPressed();

        public void Tick() => _state.Tick();
        void SetState(State state) => _state = state;

        abstract class State
        {
            protected EnhancedReactionController controller;
            public State(EnhancedReactionController con) => controller = con;
            public abstract void CoinInserted();
            public abstract void GoStopPressed();
            public abstract void Tick();
        }

        class State_On : State
        {
            public State_On(EnhancedReactionController con) : base(con)
            {
                controller.Games = 0;
                controller.TotalReactionTime = 0;
                controller.Gui.SetDisplay("Insert coin");
            }
            public override void CoinInserted() => controller.SetState(new State_Ready(controller));
            public override void GoStopPressed() { }
            public override void Tick() { }
        }

        class State_Ready : State
        {
            public State_Ready(EnhancedReactionController con) : base(con)
            {
                controller.Gui.SetDisplay("Press Go!");
                controller.Ticks = 0;
            }
            public override void CoinInserted() { }
            public override void GoStopPressed()
            {
                controller.SetState(new State_Wait(controller));
            }
            public override void Tick()
            {
                controller.Ticks++;
                if (controller.Ticks == MAX_READY_TIME)
                    controller.SetState(new State_On(controller));
            }
        }

        class State_Wait : State
        {
            private int _waitTime;
            public State_Wait(EnhancedReactionController con) : base(con)
            {
                controller.Gui.SetDisplay("Wait...");
                controller.Ticks = 0;
                _waitTime = controller.Rng.GetRandom(MIN_WAIT_TIME, MAX_WAIT_TIME);
            }
            public override void CoinInserted() { }
            public override void GoStopPressed() => controller.SetState(new State_On(controller));
            public override void Tick()
            {
                controller.Ticks++;
                if (controller.Ticks == _waitTime)
                {
                    controller.Games++;
                    controller.SetState(new State_Running(controller));
                }
            }
        }

        class State_Running : State
        {
            public State_Running(EnhancedReactionController con) : base(con)
            {
                controller.Gui.SetDisplay("0.00");
                controller.Ticks = 0;
            }
            public override void CoinInserted() { }

            public override void GoStopPressed()
            {
                controller.TotalReactionTime += controller.Ticks;
                controller.SetState(new State_GameOver(controller));
            }
            public override void Tick()
            {
                controller.Ticks++;
                controller.Gui.SetDisplay(
                (controller.Ticks / TICKS_PER_SECOND).ToString("0.00"));
                if (controller.Ticks == MAX_GAME_TIME)
                    controller.SetState(new State_GameOver(controller));
            }
        }

        class State_GameOver : State
        {
            public State_GameOver(EnhancedReactionController con) : base(con)
            {
                controller.Ticks = 0;
            }
            public override void CoinInserted() { }
            public override void GoStopPressed() => CheckGames();
            public override void Tick()
            {
                controller.Ticks++;
                if (controller.Ticks == GAMEOVER_TIME)
                    CheckGames();
            }
            private void CheckGames()
            {
                if (controller.Games == 3)
                {
                    controller.SetState(new DisplayResult(controller));
                    return;
                }
                controller.SetState(new State_Wait(controller));
            }
        }

        class DisplayResult : State
        {
            public DisplayResult(EnhancedReactionController con) : base(con)
            {
                controller.Gui.SetDisplay("Average: " + ((double)controller.TotalReactionTime / controller.Games * 0.01).ToString("0.00"));
                controller.Ticks = 0;
            }
            public override void CoinInserted() { }

            public override void GoStopPressed() => controller.SetState(new State_On(controller));
            public override void Tick()
            {
                controller.Ticks++;
                if (controller.Ticks == RESULTS_TIME)
                    controller.SetState(new State_On(controller));

            }
        }
    }
}
