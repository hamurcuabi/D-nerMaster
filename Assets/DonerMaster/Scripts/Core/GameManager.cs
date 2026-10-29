using System.Collections.Generic;
using UnityEngine;

namespace DonerMaster
{
    public class DayStats
    {
        public int Day, Served, Perfect, Missed, Earned, Tips, QualitySum;
        public float BestCombo = 1f;
    }

    /// <summary>
    /// Runs the day loop (open → 3 minutes of customers → closing → results), routes every
    /// player action, pays out coins and drives the first-day tutorial.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public enum GameState { Title, Playing, Closing, DayEnd }

        public static GameManager I { get; private set; }

        public GameState State { get; private set; } = GameState.Title;
        public bool IsPlaying => State == GameState.Playing || State == GameState.Closing;
        public float TimeLeft { get; private set; }
        public float Combo { get; private set; } = 1f;
        public DayStats Stats { get; private set; } = new DayStats();

        // Scene references (set by the bootstrap)
        public DonerSpit Spit;
        public MeatTray Tray;
        public PrepBoard Board;
        public Station HeatKnob;
        public readonly List<Station> Stations = new List<Station>();
        public Vector3[] SlotPositions;

        Customer[] _slots;
        float _spawnTimer;
        bool _tutorial;
        string _tempHint;
        float _tempHintTimer;
        float _burnHintCooldown;

        void Awake()
        {
            I = this;
        }

        public void Init()
        {
            _slots = new Customer[SlotPositions.Length];
            AudioManager.Muted = SaveSystem.Data.Muted;
            Haptics.Enabled = !SaveSystem.Data.HapticsOff;
            GameUI.I.ShowTitle();
        }

        // ------------------------------------------------------------------ Day loop

        public void StartDay()
        {
            GameUI.I.HideTitle();
            GameUI.I.HideDayEnd();
            ClearCustomers();
            Board.Clear();
            Tray.ClearAll();
            Spit.ResetForDay();

            Stats = new DayStats { Day = SaveSystem.Data.Day };
            Combo = 1f;
            TimeLeft = GameData.DayLength;
            _tutorial = !SaveSystem.Data.TutorialDone && SaveSystem.Data.Day == GameData.TutorialDay;
            _spawnTimer = _tutorial ? 2.5f : 1.5f;
            State = GameState.Playing;

            AudioManager.Play(Sfx.Bell, 0.8f);
            GameUI.I.BigText("DAY " + SaveSystem.Data.Day, new Color(1f, 0.85f, 0.3f));
            CameraShake.ZoomPunch(2f);
        }

        void EndDay()
        {
            State = GameState.DayEnd;
            int stars = 0;
            int total = Stats.Served + Stats.Missed;
            if (Stats.Served > 0)
            {
                float ratio = Stats.Served / (float)Mathf.Max(1, total);
                float avgQ = Stats.QualitySum / (float)Stats.Served;
                stars = ratio >= 0.95f && avgQ >= 80f ? 3 : ratio >= 0.75f && avgQ >= 60f ? 2 : 1;
            }
            var d = SaveSystem.Data;
            d.BestDayEarnings = Mathf.Max(d.BestDayEarnings, Stats.Earned);
            d.TotalServed += Stats.Served;
            d.Day++;
            SaveSystem.Save();

            Board.Clear();
            GameUI.I.ShowDayEnd(Stats, stars);
            for (int i = 0; i < 5; i++)
            {
                int k = i;
                Juice.Delay(0.1f + i * 0.08f, () => AudioManager.Play(Sfx.Coin, 0.5f, AudioManager.Semitones(k * 2)));
            }
        }

        void ClearCustomers()
        {
            if (_slots == null) return;
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] != null)
                {
                    GameUI.I.RemoveBubble(_slots[i]);
                    Destroy(_slots[i].gameObject);
                }
                _slots[i] = null;
            }
        }

        void Update()
        {
            if (HeatKnob != null && Spit != null) HeatKnob.SetKnob(Spit.Heat);
            if (!IsPlaying) return;
            float dt = Time.deltaTime;

            if (State == GameState.Playing && !_tutorial)
            {
                TimeLeft -= dt;
                if (TimeLeft <= 0f)
                {
                    TimeLeft = 0f;
                    State = GameState.Closing;
                    GameUI.I.BigText("CLOSING TIME!", new Color(1f, 0.5f, 0.3f), 90);
                    AudioManager.Play(Sfx.Bell, 0.8f, 0.8f);
                }
            }

            if (State == GameState.Playing) UpdateSpawning(dt);
            if (State == GameState.Closing && ActiveCustomers() == 0) EndDay();

            UpdateHints(dt);
        }

        int ActiveCustomers()
        {
            int n = 0;
            foreach (var c in _slots) if (c != null) n++;
            return n;
        }

        void UpdateSpawning(float dt)
        {
            int day = SaveSystem.Data.Day;
            int active = ActiveCustomers();
            int max = _tutorial ? 1 : GameData.MaxCustomers(day);

            _spawnTimer -= dt;
            if (active == 0 && !_tutorial) _spawnTimer = Mathf.Min(_spawnTimer, 1.5f); // never leave the shop empty for long
            if (_spawnTimer > 0f || active >= max) return;

            // pick a random free slot
            var free = new List<int>();
            for (int i = 0; i < _slots.Length; i++) if (_slots[i] == null) free.Add(i);
            if (free.Count == 0) return;
            int slot = free[Random.Range(0, free.Count)];
            Spawn(slot);
            _spawnTimer = GameData.SpawnInterval(day) * Random.Range(0.8f, 1.2f);
        }

        void Spawn(int slot)
        {
            int day = SaveSystem.Data.Day;
            CustomerType type;
            Order order;
            if (_tutorial)
            {
                type = GameData.CustomerTypes[0];
                order = new Order { Product = Product.Wrap };
                order.Ingredients.Add(Ingredient.Tomato);
            }
            else
            {
                type = GameData.PickCustomerType(day);
                order = Order.Create(day, type);
            }

            var pos = SlotPositions[slot];
            var from = new Vector3(pos.x < 0.7f ? -4.5f : 4.5f, pos.y, pos.z + 0.5f);
            float patienceMult = GameData.PatienceBonus(SaveSystem.Data.Level(UpgradeId.Decor));
            var c = Customer.Create(type, order, slot, pos, from, patienceMult, OnCustomerGone);
            c.Paused = _tutorial;
            _slots[slot] = c;
            GameUI.I.AddBubble(c);
            Juice.Delay(0.9f, () => AudioManager.Play(Sfx.Bell, 0.45f, 1.1f));
        }

        void OnCustomerGone(Customer c)
        {
            if (_slots == null) return;
            for (int i = 0; i < _slots.Length; i++) if (_slots[i] == c) _slots[i] = null;
            GameUI.I.RemoveBubble(c);
        }

        // ------------------------------------------------------------------ Actions

        public void OnSliced(MeatSlice slice) { }

        public void OnSpitEmptyTouched()
        {
            TempHint("The doner is empty — TAP it to load a new one!");
            AudioManager.Play(Sfx.Buzz, 0.4f);
        }

        public void OnTrayTapped()
        {
            Juice.Punch(Tray.transform, 0.06f, 0.2f);
            if (Board.Product == Product.None)
            {
                Fail("Pick LAVASH or BREAD first!", Board.transform.position);
                return;
            }
            if (Board.Wrapped)
            {
                Fail("Already wrapped — serve it!", Board.transform.position);
                return;
            }
            int target = GameData.MeatPieces(Board.Product);
            int have = Board.Meat.Count;
            if (have >= target + 3)
            {
                Fail("That's plenty of meat!", Board.transform.position);
                return;
            }
            int need = Mathf.Max(1, target - have);
            var taken = Tray.Take(need);
            if (taken.Count == 0)
            {
                if (Tray.BurntCount > 0) Fail("Only burnt meat left — tap TRASH to clear it", Tray.transform.position);
                else Fail("No meat! Swipe DOWN on the doner to slice", Spit.transform.position + Vector3.up);
                return;
            }
            Board.AddMeat(taken);
            Haptics.Light();
        }

        public void OnBoardTapped()
        {
            if (Board.CanWrap)
            {
                Board.Wrap();
                return;
            }
            if (Board.Wrapped) TempHint("Tap a CUSTOMER to serve it!");
            else if (Board.Product == Product.None) TempHint("Start with LAVASH or BREAD");
            else if (Board.Meat.Count == 0) TempHint("Tap the TRAY to add meat");
            Juice.Punch(Board.transform, 0.04f, 0.15f);
        }

        public void OnStationTapped(Station st)
        {
            switch (st.Kind)
            {
                case StationKind.Lavash:
                case StationKind.Bread:
                    var p = st.Kind == StationKind.Lavash ? Product.Wrap : Product.Sandwich;
                    if (!Board.SetProduct(p, st.SpawnPoint))
                        Fail(Board.Wrapped ? "Serve or trash the current order" : "The board is busy!", Board.transform.position);
                    break;

                case StationKind.Ingredient:
                    if (!Board.AddIngredient(st.Ingredient, st.SpawnPoint))
                        Fail(Board.Wrapped ? "Already wrapped!" : "Start with LAVASH or BREAD", Board.transform.position);
                    break;

                case StationKind.Ayran:
                    if (!Board.AddAyran(st.SpawnPoint)) TempHint("One ayran is enough!");
                    break;

                case StationKind.Trash:
                    if (!Board.IsEmpty)
                    {
                        Board.Clear();
                        AudioManager.Play(Sfx.Whoosh, 0.7f, 0.9f);
                    }
                    else
                    {
                        int n = Tray.DiscardBurnt();
                        if (n > 0) AudioManager.Play(Sfx.Whoosh, 0.7f, 0.9f);
                        else TempHint("Nothing to throw away");
                    }
                    break;

                case StationKind.HeatKnob:
                    Spit.CycleHeat();
                    GameUI.I.Float(st.transform.position + Vector3.up * 0.3f, "HEAT " + GameData.HeatNames[Spit.Heat],
                        Spit.Heat == 2 ? new Color(1f, 0.4f, 0.2f) : Color.white, 44, 100f, 0.8f);
                    break;
            }
        }

        public void OnCustomerTapped(Customer c)
        {
            if (!c.IsWaiting) return;
            if (!Board.ReadyToServe)
            {
                GameUI.I.PunchBubble(c);
                if (Board.IsEmpty) Fail("Make the order first!", c.BubbleAnchor);
                else if (!Board.Wrapped) Fail("WRAP it first — tap the board", Board.transform.position);
                return;
            }
            Serve(c);
        }

        void Serve(Customer c)
        {
            var order = c.Order;
            var r = ScoreCalculator.Evaluate(order, Board, c.Patience01);
            int q = r.Quality;
            bool criticUnhappy = c.Type.IsCritic && q < GameData.CriticBar;

            int pay = 0, tip = 0;
            if (q >= GameData.RefuseQuality)
            {
                pay = Mathf.RoundToInt(order.Price * q / 100f * Combo);
                if (!criticUnhappy) tip = Mathf.RoundToInt(order.Price * c.Type.TipMultiplier * (q / 100f) * (q / 100f));
            }

            // Combo
            bool perfect = q >= GameData.PerfectQuality && !criticUnhappy;
            if (perfect)
            {
                Combo = Mathf.Min(GameData.ComboMax, Combo + GameData.ComboStep);
                Stats.Perfect++;
            }
            else if (q < GameData.ComboBreakQuality || criticUnhappy) Combo = 1f;
            Stats.BestCombo = Mathf.Max(Stats.BestCombo, Combo);

            // Money + stats
            int total = pay + tip;
            SaveSystem.Data.Coins += total;
            Stats.Earned += total;
            Stats.Tips += tip;
            if (q >= GameData.RefuseQuality) { Stats.Served++; Stats.QualitySum += q; }
            else Stats.Missed++;

            // Hand it over and react
            Board.SendTo(c.HandPoint);
            c.Served(criticUnhappy ? Mathf.Min(q, GameData.ComboBreakQuality - 1) : q);
            var head = c.BubbleAnchor;

            if (total > 0)
            {
                GameUI.I.Float(head, "+" + total, new Color(1f, 0.82f, 0.25f), 76);
                int dings = Mathf.Clamp(total / 6, 1, 6);
                for (int i = 0; i < dings; i++)
                {
                    int k = i;
                    Juice.Delay(0.25f + i * 0.07f, () => AudioManager.Play(Sfx.Coin, 0.5f, AudioManager.Semitones(k), 0.02f));
                }
            }

            string verdict = c.Type.IsCritic
                ? (criticUnhappy ? "\"Disappointing.\"" : "\"Magnifique!\"")
                : r.Feedback;
            var vColor = perfect ? new Color(0.45f, 1f, 0.45f) : q >= GameData.ComboBreakQuality ? Color.white : new Color(1f, 0.45f, 0.4f);
            Juice.Delay(0.15f, () => GameUI.I.Float(head + Vector3.up * 0.35f, verdict + "  " + q + "%", vColor, 48, 120f, 1.4f));

            if (perfect)
            {
                int steps = Mathf.RoundToInt((Combo - 1f) / GameData.ComboStep);
                AudioManager.Play(Sfx.Ding, 0.7f, AudioManager.Semitones(Mathf.Min(steps, 12)), 0f);
                GameUI.I.BigText(Combo > 1.15f ? "PERFECT! x" + Combo.ToString("0.0") : "PERFECT!", new Color(0.5f, 1f, 0.5f));
                CameraShake.ZoomPunch(1.6f);
                Haptics.Medium();
            }
            else if (q < GameData.RefuseQuality || criticUnhappy)
            {
                AudioManager.Play(Sfx.Buzz, 0.6f);
                CameraShake.Add(0.35f);
                Haptics.Heavy();
            }
            else Haptics.Light();

            if (c.Type.IsCritic && !criticUnhappy)
            {
                SaveSystem.Data.Coins += 20;
                Stats.Earned += 20;
                GameUI.I.BigText("CRITIC LOVED IT! +20", new Color(1f, 0.8f, 0.3f), 80);
            }

            if (_tutorial)
            {
                _tutorial = false;
                SaveSystem.Data.TutorialDone = true;
                SaveSystem.Save();
                Juice.Delay(1.2f, () => GameUI.I.BigText("YOU'RE A NATURAL!", new Color(1f, 0.85f, 0.3f), 80));
                _spawnTimer = 3f;
            }
        }

        public void OnCustomerGaveUp(Customer c)
        {
            Stats.Missed++;
            Combo = 1f;
            GameUI.I.Float(c.BubbleAnchor, "Left angry!", new Color(1f, 0.4f, 0.35f), 56);
            CameraShake.Add(0.25f);
            Haptics.Medium();
        }

        public bool BuyUpgrade(UpgradeId id)
        {
            var def = GameData.Upgrade(id);
            int lvl = SaveSystem.Data.Level(id);
            if (lvl >= def.MaxLevel) return false;
            int price = def.Prices[lvl];
            if (SaveSystem.Data.Coins < price) return false;
            SaveSystem.Data.Coins -= price;
            SaveSystem.Data.SetLevel(id, lvl + 1);
            SaveSystem.Save();
            return true;
        }

        // ------------------------------------------------------------------ Hints

        void Fail(string message, Vector3 where)
        {
            AudioManager.Play(Sfx.Buzz, 0.45f);
            GameUI.I.Float(where + Vector3.up * 0.3f, message, new Color(1f, 0.6f, 0.5f), 40, 90f, 1.1f);
            Haptics.Light();
        }

        void TempHint(string message)
        {
            _tempHint = message;
            _tempHintTimer = 2.5f;
        }

        void UpdateHints(float dt)
        {
            _burnHintCooldown -= dt;
            if (_tempHintTimer > 0f)
            {
                _tempHintTimer -= dt;
                GameUI.I.ShowHint(_tempHint);
                return;
            }

            if (_tutorial)
            {
                GameUI.I.ShowHint(TutorialHint());
                return;
            }

            if (Spit.IsEmpty)
            {
                GameUI.I.ShowHint("The doner is empty — TAP it to load a new one!");
                return;
            }
            if (Spit.AnyZoneBurning && _burnHintCooldown < 4f)
            {
                if (_burnHintCooldown <= 0f) _burnHintCooldown = 10f;
                GameUI.I.ShowHint("It's BURNING! Slice it off fast or turn the HEAT down");
                return;
            }
            GameUI.I.HideHint();
        }

        string TutorialHint()
        {
            Customer c = null;
            foreach (var s in _slots) if (s != null) c = s;

            if (Board.Meat.Count == 0 && Tray.GoodCount < 4 && !Board.Wrapped)
                return "Swipe DOWN on the doner to slice meat.\nSlow and steady = thin slices!";
            if (c == null || !c.IsWaiting) return "Nice! Your first customer is coming...";
            if (Board.Product == Product.None) return "The customer wants a WRAP — tap LAVASH";
            if (Board.Meat.Count == 0) return "Tap the TRAY to put meat on the lavash";
            foreach (var i in c.Order.Ingredients)
                if (!Board.Ingredients.Contains(i)) return "Add " + GameData.IngredientNames[(int)i].ToUpper() + " — tap its bin";
            if (!Board.Wrapped) return "Tap the BOARD to wrap it up";
            return "Tap the CUSTOMER to serve!";
        }
    }
}
