// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Soraka.cs" company="ChewyMoon">
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
// --------------------------------------------------------------------------------------------------------------------

namespace Sophies_Soraka
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.SDK.Core;
    using LeagueSharp.SDK.Core.Enumerations;
    using LeagueSharp.SDK.Core.Events;
    using LeagueSharp.SDK.Core.Extensions;
    using LeagueSharp.SDK.Core.Signals;
    using LeagueSharp.SDK.Core.UI.IMenu;
    using LeagueSharp.SDK.Core.UI.IMenu.Values;
    using LeagueSharp.SDK.Core.Wrappers;

    /// <summary>
    /// The soraka.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed. Suppression is OK here.")]
    internal class Soraka
    {
        /// <summary>
        /// Gets or sets the q.
        /// </summary>
        /// <value>
        /// The q.
        /// </value>
        public static Spell Q { get; set; }

        /// <summary>
        /// Gets or sets the w.
        /// </summary>
        /// <value>
        /// The w.
        /// </value>
        public static Spell W { get; set; }

        /// <summary>
        /// Gets or sets the e.
        /// </summary>
        /// <value>
        /// The e.
        /// </value>
        public static Spell E { get; set; }

        /// <summary>
        /// Gets or sets the r.
        /// </summary>
        /// <value>
        /// The r.
        /// </value>
        public static Spell R { get; set; }

        /// <summary>
        /// Gets or sets the menu.
        /// </summary>
        /// <value>
        /// The menu.
        /// </value>
        public static Menu Menu { get; set; }

        /// <summary>
        /// The load.
        /// </summary>
        public void Load()
        {
            Q = new Spell(SpellSlot.Q, 0x3CA);
            W = new Spell(SpellSlot.W, 0x226);
            E = new Spell(SpellSlot.E, 0x36B);
            R = new Spell(SpellSlot.R);

            Q.SetSkillshot(0.283f, 0x12C, 0x6D6, false, SkillshotType.SkillshotCircle);
            E.SetSkillshot(0.25f, 0xFA, float.MaxValue, false, SkillshotType.SkillshotCircle);

            this.CreateMenu();

            Game.OnUpdate += this.GameOnOnUpdate;
            Gapcloser.OnGapCloser += this.Gapcloser_OnGapCloser;
            InterruptableSpell.OnInterruptableTarget += this.InterruptableSpellOnOnInterruptableTarget;
        }

        /// <summary>
        /// Interruptables the spell on a target.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="interruptableTargetEventArgs">The <see cref="InterruptableSpell.InterruptableTargetEventArgs"/> instance containing the event data.</param>
        private void InterruptableSpellOnOnInterruptableTarget(object sender, InterruptableSpell.InterruptableTargetEventArgs interruptableTargetEventArgs)
        {
            if (!interruptableTargetEventArgs.Sender.IsValidTarget(E.Range) || !E.IsReady()
                || !Menu["Misc"]["EInterrupt"].GetValue<MenuBool>().Value)
            {
                return;
            }

            E.Cast(interruptableTargetEventArgs.Sender);
        }

        /// <summary>
        /// Handles the OnGapCloser event of the Gapcloser control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Gapcloser.GapCloserEventArgs"/> instance containing the event data.</param>
        private void Gapcloser_OnGapCloser(object sender, Gapcloser.GapCloserEventArgs e)
        {
            if (!e.Sender.IsValidTarget(Q.Range))
            {
                return;
            }

            var useQ = Menu["Misc"]["QOnGapcloser"].GetValue<MenuBool>().Value;
            var useE = Menu["Misc"]["EOnGapcloser"].GetValue<MenuBool>().Value;

            if (useE && E.IsReady())
            {
                E.Cast(e.Sender);
            }
            else if (useQ && Q.IsReady())
            {
                Q.Cast(e.Sender);
            }
        }


        /// <summary>
        ///     Called when the game updates.
        /// </summary>
        /// <param name="args">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void GameOnOnUpdate(EventArgs args)
        {
            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Orbwalk:
                    this.DoCombo();
                    break;
                case OrbwalkerMode.Hybrid:
                    this.DoHarass();
                    break;
            }

            Signal.Create(
                delegate(object sender, Signal.RaisedArgs raisedArgs)
                    {
                        // Get lowest target
                        var target = GameObjects.AllyHeroes.OrderBy(x => x.Health).FirstOrDefault();

                        if (target == null)
                        {
                            return;
                        }

                        if (target.HealthPercent > Menu["Ult"]["RPercent"].GetValue<MenuSlider>().Value)
                        {
                            return;
                        }

                        if (Menu["Ult"]["RSurronded"].GetValue<MenuBool>().Value
                            && !GameObjects.EnemyHeroes.Any(
                                x => x.Distance(target) < Menu["Ult"]["REnemyRadius"].GetValue<MenuSlider>().Value))
                        {
                            return;
                        }

                        R.Cast();
                        raisedArgs.Signal.Reset();
                    },
                signal =>
                Menu["Ult"]["UseR"].GetValue<MenuBool>().Value
                && GameObjects.AllyHeroes.Any(
                    x => x.HealthPercent <= Menu["Ult"]["RPercent"].GetValue<MenuSlider>().Value));

            Signal.Create(
                delegate(object sender, Signal.RaisedArgs raisedArgs)
                    {
                        // Get lowest target
                        var target = GameObjects.AllyHeroes.OrderBy(x => x.Health).FirstOrDefault();

                        if (target == null)
                        {
                            return;
                        }

                        if (target.HealthPercent > Menu["Heal"]["AllyHealthPercent"].GetValue<MenuSlider>().Value)
                        {
                            return;
                        }

                        W.Cast(target);
                        raisedArgs.Signal.Reset();

                    },
                signal =>
                Menu["Heal"]["UseW"].GetValue<MenuBool>().Value
                && ObjectManager.Player.HealthPercent > Menu["Heal"]["UseWHealth"].GetValue<MenuSlider>().Value
                && GameObjects.AllyHeroes.Any(
                    x => x.HealthPercent < Menu["Heal"]["AllyHealthPercent"].GetValue<MenuSlider>().Value));
        }

        /// <summary>
        /// Does the combo.
        /// </summary>
        private void DoCombo()
        {
            var useQ = Menu["Combo"]["UseQ"].GetValue<MenuBool>().Value;
            var useE = Menu["Combo"]["UseE"].GetValue<MenuBool>().Value;

            var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);

            if (!target.IsValidTarget())
            {
                return;
            }

            if (useQ && Q.IsReady())
            {
                Q.Cast(target);
            }

            if (useE && E.IsReady())
            {
                E.Cast(target);
            }
        }

        /// <summary>
        /// Does the harass.
        /// </summary>
        private void DoHarass()
        {
            var useQ = Menu["Harass"]["UseQ"].GetValue<MenuBool>().Value;
            var useE = Menu["Harass"]["UseE"].GetValue<MenuBool>().Value;

            var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);

            if (!target.IsValidTarget())
            {
                return;
            }

            if (useQ && Q.IsReady())
            {
                Q.Cast(target);
            }

            if (useE && E.IsReady())
            {
                E.Cast(target);
            }
        }

        /// <summary>
        /// Creates the menu.
        /// </summary>
        private void CreateMenu()
        {
            Menu = new Menu("SSoraka", "Sophie's Soraka", true);

            var comboMenu = new Menu("Combo", "Combo");
            comboMenu.Add(new MenuBool("UseQ", "Use Q", true));
            comboMenu.Add(new MenuBool("UseE", "Use E", true));
            Menu.Add(comboMenu);

            var harassMenu = new Menu("Harass", "Harass");
            harassMenu.Add(new MenuBool("UseQ", "Use Q", true));
            harassMenu.Add(new MenuBool("UseE", "Use E", true));
            Menu.Add(harassMenu);

            var ultMenu = new Menu("Ult", "R Settings");
            ultMenu.Add(new MenuBool("UseR", "Use R", true));
            ultMenu.Add(new MenuSlider("RPercent", "Use R When Lower Than %", 0x19));
            ultMenu.Add(new MenuBool("RSurronded", "Only R When Nearby Enemies", true));
            ultMenu.Add(new MenuSlider("REnemyRadius", "Nearby Enemy Distance", 0x3E8, 0x1F4, 0x7D0));
            Menu.Add(ultMenu);

            var wMenu = new Menu("Heal", "W Settings");
            wMenu.Add(new MenuBool("UseW", "Use W", true));
            wMenu.Add(new MenuSlider("AllyHealthPercent", "Use W When Lower Than %", 0x28));
            wMenu.Add(new MenuSlider("WOnDamage", "Use W when Damaged for %", 0xA));
            wMenu.Add(new MenuSlider("UseWHealth", "Don't W When Lower Than %", 0x23));
            Menu.Add(wMenu);

            var drawingMenu = new Menu("Drawing", "Drawing");
            drawingMenu.Add(new MenuBool("DrawQ", "Draw Q", true));
            drawingMenu.Add(new MenuBool("DrawW", "Draw W", true));
            drawingMenu.Add(new MenuBool("DrawE", "Draw E", true));
            Menu.Add(drawingMenu);

            var miscMenu = new Menu("Misc", "Misc");
            miscMenu.Add(new MenuBool("QOnGapcloser", "Use Q on Gapcloser"));
            miscMenu.Add(new MenuBool("EOnGapcloser", "Use E on Gapcloser", true));
            miscMenu.Add(new MenuBool("EInterrupt", "Use E To Interrupt", true));
            Menu.Add(miscMenu);

            Menu.Attach();
        }

    }
}
