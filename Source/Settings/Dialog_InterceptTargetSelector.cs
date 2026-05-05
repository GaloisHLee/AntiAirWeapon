using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace AntiAirWeapon.Settings
{
    public class Dialog_InterceptTargetSelector : Window
    {
        private const float RowHeight = 66f;
        private const float HeaderHeight = 116f;
        private readonly AntiAirWeaponMod mod;
        private readonly InterceptRuleGroup group;
        private readonly QuickSearchWidget searchWidget = new QuickSearchWidget();
        private readonly List<InterceptCandidate> candidates = new List<InterceptCandidate>();
        private Vector2 scrollPosition;
        private CandidateFilter activeFilter = CandidateFilter.All;

        public Dialog_InterceptTargetSelector(AntiAirWeaponMod mod, InterceptRuleGroup group)
        {
            this.mod = mod;
            this.group = group;
            this.forcePause = true;
            this.doCloseX = true;
            this.absorbInputAroundWindow = true;
            this.closeOnClickedOutside = false;
            this.RebuildCandidates();
        }

        public override Vector2 InitialSize => new Vector2(920f, 720f);

        public override QuickSearchWidget CommonSearchWidget => this.searchWidget;

        public override void DoWindowContents(Rect inRect)
        {
            if (this.mod == null)
            {
                Widgets.Label(inRect, "Anti-Air Weapon settings are not initialized.");
                return;
            }

            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(inRect.x, inRect.y, inRect.width, 34f);
            Widgets.Label(titleRect, this.mod.GetRuleTitle(this.group));
            Text.Font = GameFont.Small;

            Rect summaryRect = new Rect(inRect.x, titleRect.yMax + 4f, inRect.width, 24f);
            int selectedCount = this.mod.GetRuleDefNames(this.group).Count;
            int missingCount = this.mod.CountMissingRuleDefNames(this.group);
            Widgets.Label(summaryRect, "已选择: " + selectedCount + "    缺失项: " + missingCount);

            Rect searchRect = new Rect(inRect.x, summaryRect.yMax + 6f, inRect.width, 32f);
            this.searchWidget.OnGUI(searchRect, () => this.scrollPosition = Vector2.zero, null);

            Rect filterRect = new Rect(inRect.x, searchRect.yMax + 8f, inRect.width, 30f);
            this.DrawFilterButtons(filterRect);

            Rect listRect = new Rect(inRect.x, inRect.y + HeaderHeight, inRect.width, inRect.height - HeaderHeight - 40f);
            this.DrawCandidateList(listRect);

            Rect footerRect = new Rect(inRect.x, listRect.yMax + 8f, inRect.width, 32f);
            if (Widgets.ButtonText(new Rect(footerRect.xMax - 120f, footerRect.y, 120f, 30f), "关闭"))
            {
                this.Close();
            }
        }

        private void DrawFilterButtons(Rect rect)
        {
            CandidateFilter[] filters =
            {
                CandidateFilter.All,
                CandidateFilter.Projectile,
                CandidateFilter.Skyfaller,
                CandidateFilter.Recent,
                CandidateFilter.Selected,
                CandidateFilter.Missing
            };
            float buttonWidth = rect.width / filters.Length;
            for (int i = 0; i < filters.Length; i++)
            {
                CandidateFilter filter = filters[i];
                Rect buttonRect = new Rect(rect.x + buttonWidth * i + 2f, rect.y, buttonWidth - 4f, rect.height);
                bool active = this.activeFilter == filter;
                Color oldColor = GUI.color;
                if (active)
                {
                    GUI.color = new Color(0.55f, 0.82f, 1f);
                }
                if (Widgets.ButtonText(buttonRect, GetFilterLabel(filter)))
                {
                    this.activeFilter = filter;
                    this.scrollPosition = Vector2.zero;
                }
                GUI.color = oldColor;
            }
        }

        private void DrawCandidateList(Rect rect)
        {
            List<InterceptCandidate> filtered = this.GetFilteredCandidates().ToList();
            int totalMatches = filtered.Count;
            List<InterceptCandidate> displayed = filtered.Take(InterceptTargetRegistry.MaxDisplayedCandidates).ToList();

            Rect noteRect = new Rect(rect.x, rect.y, rect.width, 24f);
            if (totalMatches > displayed.Count)
            {
                Widgets.Label(noteRect, "找到 " + totalMatches + " 项，显示前 " + displayed.Count + " 项。继续输入搜索词可缩小范围。");
            }
            else
            {
                Widgets.Label(noteRect, "找到 " + totalMatches + " 项。");
            }

            Rect outRect = new Rect(rect.x, noteRect.yMax + 4f, rect.width, rect.height - 28f);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, Mathf.Max(outRect.height, displayed.Count * RowHeight));
            Widgets.BeginScrollView(outRect, ref this.scrollPosition, viewRect, true);
            for (int i = 0; i < displayed.Count; i++)
            {
                Rect rowRect = new Rect(0f, i * RowHeight, viewRect.width, RowHeight - 4f);
                this.DrawCandidateRow(rowRect, displayed[i]);
            }
            Widgets.EndScrollView();
        }

        private void DrawCandidateRow(Rect rect, InterceptCandidate candidate)
        {
            if (candidate == null)
            {
                return;
            }

            Widgets.DrawHighlightIfMouseover(rect);
            Widgets.DrawBox(rect);

            bool selectedHere = this.mod.GetRuleDefNameSet(this.group).Contains(candidate.DefName);
            InterceptRuleGroup? existingGroup = this.mod.GetRuleForDefName(candidate.DefName);
            bool selectedElsewhere = existingGroup.HasValue && existingGroup.Value != this.group;

            Rect kindRect = new Rect(rect.x + 8f, rect.y + 7f, 132f, 22f);
            this.DrawKindLabel(kindRect, candidate);

            Rect labelRect = new Rect(kindRect.xMax + 8f, rect.y + 5f, rect.width - 300f, 24f);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, candidate.Label ?? candidate.DefName);

            Rect detailRect = new Rect(labelRect.x, labelRect.yMax + 2f, labelRect.width, 20f);
            GUI.color = Color.gray;
            Widgets.Label(detailRect, candidate.DefName + "  ·  " + (candidate.SourceModName ?? "Unknown"));
            GUI.color = Color.white;

            Rect statusRect = new Rect(labelRect.x, detailRect.yMax + 1f, labelRect.width, 18f);
            if (candidate.IsMissing)
            {
                GUI.color = new Color(1f, 0.55f, 0.45f);
                Widgets.Label(statusRect, "缺失项: 当前 mod 列表中找不到这个 defName");
                GUI.color = Color.white;
            }
            else if (candidate.IsObservedRecently)
            {
                GUI.color = new Color(0.8f, 1f, 0.55f);
                Widgets.Label(statusRect, "最近检测到");
                GUI.color = Color.white;
            }

            Rect actionRect = new Rect(rect.xMax - 128f, rect.y + 16f, 118f, 32f);
            string buttonLabel = selectedHere ? "删除" : (selectedElsewhere ? "移动到这里" : "添加");
            if (Widgets.ButtonText(actionRect, buttonLabel))
            {
                if (selectedHere)
                {
                    this.mod.RemoveRuleDefName(this.group, candidate.DefName);
                }
                else
                {
                    this.mod.AddOrMoveRuleDefName(this.group, candidate.DefName);
                }
                this.RebuildCandidates();
            }

            if (selectedElsewhere)
            {
                TooltipHandler.TipRegion(actionRect, "已在“" + this.mod.GetRuleTitle(existingGroup.Value) + "”中。点击后会自动移动到当前列表。");
            }
            if (!string.IsNullOrEmpty(candidate.Description))
            {
                TooltipHandler.TipRegion(rect, candidate.Description);
            }

            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }

        private void DrawKindLabel(Rect rect, InterceptCandidate candidate)
        {
            Color oldColor = GUI.color;
            switch (candidate.Kind)
            {
                case InterceptCandidateKind.OverheadProjectile:
                    GUI.color = new Color(0.7f, 0.9f, 1f);
                    break;
                case InterceptCandidateKind.Projectile:
                    GUI.color = new Color(0.85f, 0.85f, 1f);
                    break;
                case InterceptCandidateKind.Skyfaller:
                    GUI.color = new Color(0.85f, 1f, 0.75f);
                    break;
                case InterceptCandidateKind.DropPodTransporter:
                    GUI.color = new Color(1f, 0.9f, 0.6f);
                    break;
                default:
                    GUI.color = candidate.IsMissing ? new Color(1f, 0.55f, 0.45f) : Color.gray;
                    break;
            }
            Widgets.Label(rect, candidate.KindLabel);
            GUI.color = oldColor;
        }

        private IEnumerable<InterceptCandidate> GetFilteredCandidates()
        {
            string searchText = this.searchWidget.filter != null ? this.searchWidget.filter.Text : string.Empty;
            HashSet<string> selected = this.mod.GetRuleDefNameSet(this.group);
            for (int i = 0; i < this.candidates.Count; i++)
            {
                InterceptCandidate candidate = this.candidates[i];
                if (candidate == null || !candidate.MatchesTokens(searchText))
                {
                    continue;
                }

                bool isSelected = selected.Contains(candidate.DefName);
                if (!this.PassesActiveFilter(candidate, isSelected))
                {
                    continue;
                }

                yield return candidate;
            }
        }

        private bool PassesActiveFilter(InterceptCandidate candidate, bool isSelected)
        {
            switch (this.activeFilter)
            {
                case CandidateFilter.Projectile:
                    return candidate.Kind == InterceptCandidateKind.Projectile || candidate.Kind == InterceptCandidateKind.OverheadProjectile;
                case CandidateFilter.Skyfaller:
                    return candidate.Kind == InterceptCandidateKind.Skyfaller || candidate.Kind == InterceptCandidateKind.DropPodTransporter;
                case CandidateFilter.Recent:
                    return candidate.IsObservedRecently;
                case CandidateFilter.Selected:
                    return isSelected;
                case CandidateFilter.Missing:
                    return candidate.IsMissing;
                default:
                    return true;
            }
        }

        private void RebuildCandidates()
        {
            this.candidates.Clear();
            if (this.mod == null)
            {
                return;
            }
            this.candidates.AddRange(InterceptTargetRegistry.BuildCandidates(this.mod.GetAllRuleDefNames(), this.mod.GetObservedAirTargets()));
        }

        private static string GetFilterLabel(CandidateFilter filter)
        {
            switch (filter)
            {
                case CandidateFilter.Projectile:
                    return "炮弹";
                case CandidateFilter.Skyfaller:
                    return "飞行舱";
                case CandidateFilter.Recent:
                    return "最近检测";
                case CandidateFilter.Selected:
                    return "已选择";
                case CandidateFilter.Missing:
                    return "缺失项";
                default:
                    return "全部";
            }
        }

        private enum CandidateFilter
        {
            All,
            Projectile,
            Skyfaller,
            Recent,
            Selected,
            Missing
        }
    }
}
