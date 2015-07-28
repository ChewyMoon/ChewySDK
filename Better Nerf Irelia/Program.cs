// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="ChewyMoon">
//   Copyright (C) 2015 ChewyMoon
//   
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General Public License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//   
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//   GNU General Public License for more details.
//   
//   You should have received a copy of the GNU General Public License
//   along with this program.  If not, see <http://www.gnu.org/licenses/>.
// </copyright>
// <summary>
//   The program.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Better_Nerf_Irelia
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Drawing;
    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.SDK.Core;
    using LeagueSharp.SDK.Core.Enumerations;
    using LeagueSharp.SDK.Core.Events;
    using LeagueSharp.SDK.Core.Extensions;
    using LeagueSharp.SDK.Core.UI.IMenu;
    using LeagueSharp.SDK.Core.UI.IMenu.Values;
    using LeagueSharp.SDK.Core.Utils;
    using LeagueSharp.SDK.Core.Wrappers;

    /// <summary>
    ///     The program.
    /// </summary>
    internal class Program
    {
        #region Properties

        /// <summary>
        ///     Gets or sets the botrk item.
        /// </summary>
        /// <value>
        ///     The botrk.
        /// </value>
        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", 
            Justification = "Reviewed. Suppression is OK here.")]
        private static Items.Item Botrk { get; set; }

        /// <summary>
        ///     Gets or sets the e.
        /// </summary>
        /// <value>
        ///     The e.
        /// </value>
        private static Spell E { get; set; }

        /// <summary>
        ///     Gets or sets the hextech item.
        /// </summary>
        /// <value>
        ///     The hextech.
        /// </value>
        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", 
            Justification = "Reviewed. Suppression is OK here.")]
        private static Items.Item Hextech { get; set; }

        /// <summary>
        ///     Gets or sets the last Q time.
        /// </summary>
        /// <value>
        ///     The last Q time.
        /// </value>
        private static int LastQTime { get; set; }

        /// <summary>
        ///     Gets or sets the menu.
        /// </summary>
        /// <value>
        ///     The menu.
        /// </value>
        private static Menu Menu { get; set; }

        /// <summary>
        ///     Gets the player.
        /// </summary>
        /// <value>
        ///     The player.
        /// </value>
        private static Obj_AI_Hero Player
        {
            get
            {
                return ObjectManager.Player;
            }
        }

        /// <summary>
        ///     Gets or sets the q.
        /// </summary>
        /// <value>
        ///     The q.
        /// </value>
        private static Spell Q { get; set; }

        /// <summary>
        ///     Gets or sets the r.
        /// </summary>
        /// <value>
        ///     The r.
        /// </value>
        private static Spell R { get; set; }

        /// <summary>
        ///     Gets or sets the randuins item.
        /// </summary>
        /// <value>
        ///     The randuins.
        /// </value>
        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", 
            Justification = "Reviewed. Suppression is OK here.")]
        private static Items.Item Randuins { get; set; }

        /// <summary>
        ///     Gets a value indicating whether Irelia's ult is activated.
        /// </summary>
        /// <value>
        ///     <c>true</c> if Irelia's ult is activated; otherwise, <c>false</c>.
        /// </value>
        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", 
            Justification = "Reviewed. Suppression is OK here.")]
        private static bool UltActivated
        {
            get
            {
                return Player.HasBuff("ireliatranscendentbladesspell");
            }
        }

        /// <summary>
        ///     Gets or sets the w.
        /// </summary>
        /// <value>
        ///     The w.
        /// </value>
        private static Spell W { get; set; }

        #endregion

        #region Methods

        /// <summary>
        ///     Creates the menu.
        /// </summary>
        private static void CreateMenu()
        {
            Menu = new Menu("BetterNerfIreliaCM", "Better Nerf Irelia", true);

            var comboMenu = new Menu("Combo", "Combo");
            comboMenu.Add(new MenuSeparator("QSettings", "Q Settings"));
            comboMenu.Add(new MenuBool("UseQ", "Use Q", true));
            comboMenu.Add(new MenuBool("UseQGapclose", "Gapclose with Q with Minions", true));
            comboMenu.Add(new MenuBool("UseQGapcloseInfinite", "Gapclose More than Once", true));
            comboMenu.Add(new MenuSeparator("WSettings", "W Settings"));
            comboMenu.Add(new MenuBool("UseW", "Use W", true));
            comboMenu.Add(new MenuSeparator("ESettings", "E Settings"));
            comboMenu.Add(new MenuBool("UseE", "Use E", true));
            comboMenu.Add(new MenuBool("UseEStunOnly", "Only Use E to Stun"));
            comboMenu.Add(new MenuSeparator("UltSettings", "Ult Settings"));
            comboMenu.Add(new MenuBool("UseR", "Use R", true));
            comboMenu.Add(new MenuBool("ProcSheenBeforeR", "Proc Sheen Before Casting R", true));
            comboMenu.Add(new MenuBool("UseRToQ", "Use R to Weaken Minions to Q Gapclose", true));
            comboMenu.Add(new MenuSeparator("ItemSettings", "Item Settings"));
            comboMenu.Add(new MenuBool("UseBotrk", "Use Blade of the Ruined King", true));
            comboMenu.Add(new MenuBool("UseRanduin", "Use Randuin's Omen", true));
            comboMenu.Add(new MenuBool("UseHextech", "Use Hextech Gunblade", true));
            comboMenu.Add(new MenuSeparator("MiscSettings", "Misc"));
            comboMenu.Add(new MenuList<string>("Mode", "Combo Mode", new[] { "Q -> W -> E", "Q -> E -> W" }));
            Menu.Add(comboMenu);

            var harassMenu = new Menu("Harass", "Harass");
            harassMenu.Add(new MenuBool("UseQ", "Use Q", true));
            harassMenu.Add(new MenuBool("UseW", "Use W", true));
            harassMenu.Add(new MenuBool("UseE", "Use E", true));
            harassMenu.Add(new MenuList<string>("Mode", "Harass Mode", new[] { "Q -> W -> E", "Q -> E -> W" }));
            Menu.Add(harassMenu);

            var killStealMenu = new Menu("KillSteal", "Kill Steal");
            killStealMenu.Add(new MenuBool("UseQ", "Use Q", true));
            killStealMenu.Add(new MenuBool("UseE", "Use E", true));
            killStealMenu.Add(new MenuBool("UseR", "Use R", true));
            killStealMenu.Add(new MenuBool("ActivateRToKS", "Activate R to KS"));
            Menu.Add(killStealMenu);

            var lastHitMenu = new Menu("LastHit", "Last Hit");
            lastHitMenu.Add(new MenuBool("UseQ", "Use Q"));
            lastHitMenu.Add(new MenuSlider("QMana", "Q Mana Percent", 0x32));
            lastHitMenu.Add(new MenuBool("QUnderTower", "Q Minion Under Tower"));
            lastHitMenu.Add(new MenuSlider("QDelay", "Q Delay (MS)", 0x32, 0x0, 0x5DC));
            Menu.Add(lastHitMenu);

            var waveClearMenu = new Menu("WaveClear", "Wave Clear");
            waveClearMenu.Add(new MenuBool("UseQ", "Use Q", true));
            waveClearMenu.Add(new MenuBool("UseW", "Use W", true));
            waveClearMenu.Add(new MenuBool("UseE", "Use E"));
            waveClearMenu.Add(new MenuBool("UseR", "Use R"));
            waveClearMenu.Add(new MenuSlider("QDelay", "Q Delay (MS)", 0x32, 0x0, 0x5DC));
            Menu.Add(waveClearMenu);

            var drawingMenu = new Menu("Drawing", "Drawing");
            drawingMenu.Add(new MenuBool("DrawQ", "Draw Q", true));
            drawingMenu.Add(new MenuBool("DrawE", "Draw E", true));
            drawingMenu.Add(new MenuBool("DrawR", "Draw R", true));
            drawingMenu.Add(new MenuBool("DrawKillableMinion", "Draw Minions Killable with Q", true));
            drawingMenu.Add(new MenuBool("DrawStunnable", "Draw Stunnable", true));
            Menu.Add(drawingMenu);

            var miscMenu = new Menu("Misc", "Misc");
            miscMenu.Add(new MenuSeparator("InterruptSettings", "Interrupter Settings"));
            miscMenu.Add(new MenuBool("UseEInterrupt", "Use E", true));
            miscMenu.Add(new MenuBool("QEInterrupt", "Use Q + E to Interrupt", true));
            miscMenu.Add(new MenuSeparator("GapcloserSettings", "Gapcloser Settings"));
            miscMenu.Add(new MenuBool("UseEGapcloser", "Use E", true));
            Menu.Add(miscMenu);

            Menu.Attach();
        }

        /// <summary>
        ///     Does the combo.
        /// </summary>
        private static void DoCombo()
        {
            var target = TargetSelector.GetTarget(Q.Range);

            var useQ = Menu["Combo"]["UseQ"].GetValue<MenuBool>().Value;
            var useQGapclose = Menu["Combo"]["UseQGapclose"].GetValue<MenuBool>().Value;
            var useQGapcloseInfinite = Menu["Combo"]["UseQGapcloseInfinite"].GetValue<MenuBool>().Value;
            var useW = Menu["Combo"]["UseW"].GetValue<MenuBool>().Value;
            var useE = Menu["Combo"]["UseE"].GetValue<MenuBool>().Value;
            var useEStunOnly = Menu["Combo"]["UseEStunOnly"].GetValue<MenuBool>().Value;
            var useR = Menu["Combo"]["UseR"].GetValue<MenuBool>().Value;
            var procSheenBeforeR = Menu["Combo"]["ProcSheenBeforeR"].GetValue<MenuBool>().Value;
            var useRtoQ = Menu["Combo"]["UseRToQ"].GetValue<MenuBool>().Value;
            var useBotrk = Menu["Combo"]["UseBotrk"].GetValue<MenuBool>().Value;
            var useRanduin = Menu["Combo"]["UseRanduin"].GetValue<MenuBool>().Value;
            var useHextech = Menu["Combo"]["UseHextech"].GetValue<MenuBool>().Value;
            var mode = Menu["Combo"]["Mode"].GetValue<MenuList<string>>().SelectedValue;

            if (target == null && useQGapclose)
            {
                var gapcloseMinion = useQGapcloseInfinite
                                         ? GameObjects.EnemyMinions.Where(
                                             x =>
                                             x.IsValidTarget(Q.Range)
                                             && Player.GetSpellDamage(x, SpellSlot.Q) > x.Health
                                             && Player.Distance(
                                                 GameObjects.EnemyHeroes.OrderBy(y => y.Distance(Player))
                                                    .FirstOrDefault())
                                             < x.Distance(
                                                 GameObjects.EnemyHeroes.OrderBy(y => y.Distance(Player))
                                                   .FirstOrDefault()))
                                               .FirstOrDefault(
                                                   x =>
                                                   x.Distance(
                                                       GameObjects.EnemyHeroes.OrderBy(y => y.Distance(Player))
                                                       .FirstOrDefault()) < Q.Range)
                                         : GameObjects.EnemyMinions.Where(
                                             x =>
                                             x.IsValidTarget(Q.Range)
                                             && Player.GetSpellDamage(x, SpellSlot.Q) > x.Health)
                                               .FirstOrDefault(
                                                   x => x.Distance(TargetSelector.GetTarget(Q.Range * 5)) < Q.Range);

                if (gapcloseMinion != null)
                {
                    Q.CastOnUnit(gapcloseMinion);
                }
                else if (useRtoQ && R.IsReady())
                {
                    var minionR =
                        ObjectManager.Get<Obj_AI_Minion>()
                            .Where(
                                x =>
                                x.IsValidTarget() && x.Distance(Player) < Q.Range
                                && GameObjects.EnemyHeroes.Count(y => x.Distance(y) < Q.Range) > 0)
                            .FirstOrDefault(
                                x =>
                                x.Health - Player.GetSpellDamage(x, SpellSlot.R) < Player.GetSpellDamage(x, SpellSlot.Q));

                    if (minionR != null)
                    {
                        R.Cast(minionR);
                    }
                }

                return;
            }

            if (!target.IsValidTarget())
            {
                return;
            }

            if (useBotrk && Botrk.IsReady)
            {
                Botrk.Cast(target);
            }

            if (useRanduin && Randuins.IsReady && Player.Distance(target) < 500
                && Player.Distance(target) > Player.GetRealAutoAttackRange())
            {
                Randuins.Cast();
            }

            if (useHextech && Hextech.IsReady)
            {
                Hextech.Cast(target);
            }

            if (useQ && Q.IsReady())
            {
                Q.CastOnUnit(target);
            }

            if (mode.Equals("Q -> W -> E"))
            {
                if (useW && W.IsReady())
                {
                    W.Cast();
                }

                if (useE && E.IsReady())
                {
                    UseE(target, useEStunOnly);
                }
            }
            else
            {
                if (useE && E.IsReady())
                {
                    UseE(target, useEStunOnly);
                }

                if (useW && W.IsReady())
                {
                    W.Cast();
                }
            }

            if (!useR || !R.IsReady())
            {
                return;
            }

            var ultTarget = TargetSelector.GetTarget(R.Range);

            if (procSheenBeforeR)
            {
                // Cast ult on the target if we have the sheen buff only if they are out of the AA range
                if (Player.Distance(ultTarget) > Player.GetRealAutoAttackRange() || !Player.HasBuff("sheen"))
                {
                    R.Cast(ultTarget);
                }
            }
            else if (!procSheenBeforeR)
            {
                R.Cast(target);
            }
        }

        /// <summary>
        ///     Does the harass.
        /// </summary>
        private static void DoHarass()
        {
            var target = TargetSelector.GetTarget(Q.Range);

            if (!target.IsValidTarget())
            {
                return;
            }

            var useQ = Menu["Harass"]["UseQ"].GetValue<MenuBool>().Value;
            var useW = Menu["Harass"]["UseW"].GetValue<MenuBool>().Value;
            var useE = Menu["Harass"]["UseE"].GetValue<MenuBool>().Value;
            var mode = Menu["Harass"]["Mode"].GetValue<MenuList<string>>().SelectedValue;

            if (useQ && Q.IsReady())
            {
                Q.CastOnUnit(target);
            }

            if (mode.Equals("Q -> W -> E"))
            {
                if (useW && W.IsReady())
                {
                    W.Cast();
                }

                if (useE && E.IsReady())
                {
                    UseE(target, false);
                }
            }
            else
            {
                if (useE && E.IsReady())
                {
                    UseE(target, false);
                }

                if (useW && W.IsReady())
                {
                    W.Cast();
                }
            }
        }

        /// <summary>
        ///     Does the last hitting.
        /// </summary>
        private static void DoLastHit()
        {
            var useQ = Menu["LastHit"]["UseQ"].GetValue<MenuBool>().Value;
            var qMana = Menu["LastHit"]["QMana"].GetValue<MenuSlider>().Value;
            var qUnderTower = Menu["LastHit"]["QUnderTower"].GetValue<MenuBool>().Value;
            var qDelay = Menu["LastHit"]["QDelay"].GetValue<MenuSlider>().Value;

            if (Environment.TickCount - LastQTime > qDelay)
            {
                return;
            }

            if (Player.ManaPercent < qMana)
            {
                return;
            }

            if (!useQ || !Q.IsReady())
            {
                return;
            }

            var minion =
                GameObjects.EnemyMinions.FirstOrDefault(
                    x =>
                    (qUnderTower || !x.IsUnderTurret(true)) && x.IsValidTarget(Q.Range)
                    && Player.GetSpellDamage(x, SpellSlot.Q) > x.Health);

            if (minion != null)
            {
                Q.CastOnUnit(minion);
            }
        }

        /// <summary>
        ///     Does the wave clear.
        /// </summary>
        private static void DoWaveClear()
        {
            var useQ = Menu["WaveClear"]["UseQ"].GetValue<MenuBool>().Value;
            var useW = Menu["WaveClear"]["UseW"].GetValue<MenuBool>().Value;
            var useE = Menu["WaveClear"]["UseE"].GetValue<MenuBool>().Value;
            var useR = Menu["WaveClear"]["UseR"].GetValue<MenuBool>().Value;
            var qDelay = Menu["WaveClear"]["QDelay"].GetValue<MenuSlider>().Value;

            if (useQ && Q.IsReady() && Environment.TickCount - LastQTime > qDelay)
            {
                var qTarget =
                    GameObjects.EnemyMinions.FirstOrDefault(
                        x => x.IsValidTarget(Q.Range) && Player.GetSpellDamage(x, SpellSlot.Q) > x.Health);

                if (qTarget != null)
                {
                    Q.CastOnUnit(qTarget);
                }
            }

            if (useW && W.IsReady() && GameObjects.EnemyMinions.Any(x => W.IsInRange(x)))
            {
                W.Cast();
            }

            if (useE && E.IsReady() && E.IsInRange(Orbwalker.OrbwalkTarget))
            {
                E.CastOnUnit(Orbwalker.OrbwalkTarget as Obj_AI_Base);
            }

            if (!useR || !R.IsReady())
            {
                return;
            }

            var location =
                R.GetLineFarmLocation(
                    GameObjects.EnemyMinions.Where(x => x.IsValidTarget(R.Range)).Cast<Obj_AI_Base>().ToList());

            if (location.MinionsHit > 1)
            {
                R.Cast(location.Position);
            }
        }

        /// <summary>
        /// Called when the game is being drawn.
        /// </summary>
        /// <param name="args">
        /// The <see cref="EventArgs"/> instance containing the event data.
        /// </param>
        private static void DrawingOnDraw(EventArgs args)
        {
            var drawQ = Menu["Drawing"]["DrawQ"].GetValue<MenuBool>().Value;
            var drawE = Menu["Drawing"]["DrawE"].GetValue<MenuBool>().Value;
            var drawR = Menu["Drawing"]["DrawR"].GetValue<MenuBool>().Value;
            var drawKillableMinion = Menu["Drawing"]["DrawKillableMinion"].GetValue<MenuBool>().Value;
            var drawStunnable = Menu["Drawing"]["DrawStunnable"].GetValue<MenuBool>().Value;

            if (drawQ)
            {
                Render.Circle.DrawCircle(Player.Position, Q.Range, Q.IsReady() ? Color.Aqua : Color.Red);
            }

            if (drawE)
            {
                Render.Circle.DrawCircle(Player.Position, E.Range, E.IsReady() ? Color.Aqua : Color.Red);
            }

            if (drawR)
            {
                Render.Circle.DrawCircle(Player.Position, R.Range, R.IsReady() ? Color.Aqua : Color.Red);
            }

            if (drawKillableMinion)
            {
                foreach (var minion in
                    GameObjects.EnemyMinions.Where(
                        x => x.IsValidTarget() && Player.GetSpellDamage(x, SpellSlot.Q) > x.Health))
                {
                    Render.Circle.DrawCircle(minion.Position, 100, Color.DarkCyan);
                }
            }

            if (!drawStunnable)
            {
                return;
            }

            foreach (var enemy in
                GameObjects.EnemyHeroes.Where(x => x.IsValidTarget() && x.HealthPercent > Player.HealthPercent))
            {
                Render.Circle.DrawCircle(enemy.Position, 100, Color.LimeGreen);
            }
        }

        /// <summary>
        /// Called when the game gets updated.
        /// </summary>
        /// <param name="args">
        /// The <see cref="EventArgs"/> instance containing the event data.
        /// </param>
        private static void Game_OnUpdate(EventArgs args)
        {
            KillSteal();

            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Orbwalk:
                    DoCombo();
                    break;
                case OrbwalkerMode.Hybrid:
                    DoHarass();
                    break;
                case OrbwalkerMode.LastHit:
                    DoLastHit();
                    break;
                case OrbwalkerMode.LaneClear:
                    DoWaveClear();
                    break;
            }
        }

        /// <summary>
        /// Called when there is an active gapcloser.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The <see cref="Gapcloser.GapCloserEventArgs"/> instance containing the event data.
        /// </param>
        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", 
            Justification = "Reviewed. Suppression is OK here.")]
        private static void GapcloserOnGapCloser(object sender, Gapcloser.GapCloserEventArgs e)
        {
            if (!e.Sender.IsValidTarget(E.Range))
            {
                return;
            }

            if (E.IsReady() && Menu["Misc"]["UseEGapcloser"].GetValue<MenuBool>().Value)
            {
                E.CastOnUnit(e.Sender);
            }
        }

        /// <summary>
        /// Gets the combo damage.
        /// </summary>
        /// <param name="target">
        /// The target.
        /// </param>
        /// <returns>
        /// The <see cref="float"/>.
        /// </returns>
        private static double GetComboDamage(Obj_AI_Hero target)
        {
            var damage = 0d;

            if (Q.IsReady())
            {
                damage += Player.GetSpellDamage(target, SpellSlot.Q);
            }

            if (W.IsReady() && Orbwalker.CanAttack)
            {
                damage += Player.GetSpellDamage(target, SpellSlot.W);
            }

            if (E.IsReady())
            {
                damage += Player.GetSpellDamage(target, SpellSlot.E);
            }

            if (R.IsReady())
            {
                damage += Player.GetSpellDamage(target, SpellSlot.R) * R.Instance.Ammo;
            }

            if (Orbwalker.CanAttack)
            {
                damage += Player.GetAutoAttackDamage(target, true);
            }

            return damage;
        }

        /// <summary>
        /// Called when there is an interruptable target.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The <see cref="InterruptableSpell.InterruptableTargetEventArgs"/> instance containing the event data.
        /// </param>
        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", 
            Justification = "Reviewed. Suppression is OK here.")]
        private static void InterruptableSpellOnInterruptableTarget(
            object sender, 
            InterruptableSpell.InterruptableTargetEventArgs e)
        {
            var unit = e.Sender;

            if (!unit.IsValidTarget() || unit.HealthPercent < Player.HealthPercent || !E.IsReady())
            {
                return;
            }

            var useEInterrupt = Menu["Misc"]["UseEInterrupt"].GetValue<MenuBool>().Value;
            var useFlyingInterrupt = Menu["Misc"]["QEInterrupt"].GetValue<MenuBool>().Value;

            if (useEInterrupt && E.IsInRange(unit))
            {
                E.CastOnUnit(unit);
            }

            if (!useFlyingInterrupt || E.IsInRange(unit) || !Q.IsInRange(unit) || !Q.IsReady()
                || e.DangerLevel != DangerLevel.High || GetComboDamage(unit) < unit.Health)
            {
                return;
            }

            Q.CastOnUnit(unit);
            DelayAction.Add(
                (Q.Delay * 1000) + Game.Ping + (1000 * (Player.Distance(unit) / Q.Speed)), 
                () => E.CastOnUnit(unit));
        }

        /// <summary>
        ///     Steals kills.
        /// </summary>
        private static void KillSteal()
        {
            var useQ = Menu["KillSteal"]["UseQ"].GetValue<MenuBool>().Value;
            var useE = Menu["KillSteal"]["UseE"].GetValue<MenuBool>().Value;
            var useR = Menu["KillSteal"]["UseR"].GetValue<MenuBool>().Value;
            var activateRToKs = Menu["KillSteal"]["ActivateRToKS"].GetValue<MenuBool>().Value;

            if (useE)
            {
                var eTarget =
                    GameObjects.EnemyHeroes.FirstOrDefault(
                        x => x.IsValidTarget(E.Range) && Player.GetSpellDamage(x, SpellSlot.E) > x.Health);

                if (eTarget != null)
                {
                    E.CastOnUnit(eTarget);
                    return;
                }
            }

            if (useQ)
            {
                var qTarget =
                    GameObjects.EnemyHeroes.FirstOrDefault(
                        x => x.IsValidTarget(Q.Range) && Player.GetSpellDamage(x, SpellSlot.Q) > x.Health);

                if (qTarget != null)
                {
                    Q.CastOnUnit(qTarget);
                    return;
                }
            }

            if (!useR)
            {
                return;
            }

            if (activateRToKs && !UltActivated)
            {
                return;
            }

            var rTarget =
                GameObjects.EnemyHeroes.FirstOrDefault(
                    x => x.IsValidTarget(R.Range) && Player.GetSpellDamage(x, SpellSlot.R) > x.Health);

            if (rTarget != null)
            {
                R.Cast(rTarget);
            }
        }

        /// <summary>
        /// Called when the game loads.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="eventArgs">
        /// The <see cref="EventArgs"/> instance containing the event data.
        /// </param>
        private static void LoadOnOnLoad(object sender, EventArgs eventArgs)
        {
            if (Player.CharData.BaseSkinName != "Irelia")
            {
                return;
            }

            Bootstrap.Init(null);

            Q = new Spell(SpellSlot.Q, 0x28A);
            W = new Spell(SpellSlot.W, Player.AttackRange + Player.BoundingRadius);
            E = new Spell(SpellSlot.E, 0x1A9);
            R = new Spell(SpellSlot.R, 0x4B0);

            Q.SetTargetted(0x3E876C8B, 0x898);
            E.SetTargetted(0x3E800000, 0x14);
            R.SetSkillshot(0x3E800000, 0x41, 0x640, false, SkillshotType.SkillshotLine);

            Botrk = new Items.Item(ItemId.Blade_of_the_Ruined_King, 0x226);
            Randuins = new Items.Item(ItemId.Randuins_Omen, 0x1F4);
            Hextech = new Items.Item(ItemId.Hextech_Gunblade, 0x2BC);

            CreateMenu();

            Game.OnUpdate += Game_OnUpdate;
            Obj_AI_Base.OnProcessSpellCast += ObjAiBaseOnProcessSpellCast;
            Drawing.OnDraw += DrawingOnDraw;
            InterruptableSpell.OnInterruptableTarget += InterruptableSpellOnInterruptableTarget;
            Gapcloser.OnGapCloser += GapcloserOnGapCloser;
        }

        /// <summary>
        /// The main.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        private static void Main(string[] args)
        {
            Load.OnLoad += LoadOnOnLoad;
        }

        /// <summary>
        /// Called when the client processes a spell cast.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="args">
        /// The <see cref="GameObjectProcessSpellCastEventArgs"/> instance containing the event data.
        /// </param>
        private static void ObjAiBaseOnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe)
            {
                return;
            }

            if (args.SData.Name == "IreliaGatotsu")
            {
                LastQTime = Environment.TickCount;
            }
        }

        /// <summary>
        /// Uses the E spell.
        /// </summary>
        /// <param name="target">
        /// The target.
        /// </param>
        /// <param name="onlyIfStunnable">
        /// if set to <c>true</c> will only cast E if the target is stunnable.
        /// </param>
        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", 
            Justification = "Reviewed. Suppression is OK here.")]
        private static void UseE(Obj_AI_Base target, bool onlyIfStunnable)
        {
            if (onlyIfStunnable && Player.HealthPercent > target.HealthPercent)
            {
                return;
            }

            E.CastOnUnit(target);
        }

        #endregion
    }
}