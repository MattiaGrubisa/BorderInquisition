using System;
using System.Linq;
using Diplomacy;
using Gameplay;
using Gameplay.Managers;
using TMPro;
using UnityEngine;

namespace UI
{
    // The current player's diplomacy: answer the offers waiting for them, offer pacts and alliances,
    // and break treaties (asking twice, since breaking marks them a traitor). The rules all live in
    // GameController.Diplomacy; this panel only shows what it allows.
    public class DiplomacyPanel : MonoBehaviour
    {
        private const float RowHeight = 42f;
        private const float Width = 900f;

        private TMP_Text _title;
        private Transform _offerRows;
        private Transform _playerRows;
        private Player _confirmingBreak;
        private Action _onChanged;

        public bool IsOpen => gameObject.activeSelf;

        private static GameController Game => GameController.Instance;
        private static DiplomacySystem Diplomacy => Game.Diplomacy;

        public static DiplomacyPanel Create(Transform canvas, Action onChanged)
        {
            var root = UiFactory.Panel(canvas, "DiplomacyPanel", new Vector2(0f, 0.5f));
            root.anchoredPosition = new Vector2(20f, 0f);

            var panel = root.gameObject.AddComponent<DiplomacyPanel>();
            panel._onChanged = onChanged;
            panel.Build(root);
            root.gameObject.SetActive(false);
            return panel;
        }

        private void Build(Transform root)
        {
            _title = UiFactory.Label(root, "-", 30f, Width, 40f);
            UiFactory.Label(root, "Offers to you", 26f, Width, 36f);
            _offerRows = UiFactory.Column(root, "OfferRows");
            UiFactory.Label(root, "Players", 26f, Width, 36f);
            _playerRows = UiFactory.Column(root, "PlayerRows");
            UiFactory.Button(root, "Close", Width, 52f, Close);
        }

        public void Open()
        {
            _confirmingBreak = null;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Refresh();
        }

        public void Close() => gameObject.SetActive(false);

        private void Refresh()
        {
            var me = Game.CurrentPlayer;
            _title.text = $"Diplomacy - {Format.Name(me)}";

            UiFactory.Clear(_offerRows);
            var offers = Diplomacy.OffersTo(me).ToList();
            if (offers.Count == 0)
                UiFactory.Label(_offerRows, "None - offers wait for your turn and lapse when it ends.", 22f, Width, RowHeight);
            foreach (var offer in offers)
                OfferRow(me, offer);

            UiFactory.Clear(_playerRows);
            foreach (var other in Game.Players.Where(p => p != me && !Game.IsEliminated(p)))
                PlayerRow(me, other);
        }

        private void OfferRow(Player me, Offer offer)
        {
            var row = UiFactory.Row(_offerRows, "Offer");
            UiFactory.Label(row, $"{Format.Name(offer.From)} offers {Describe(offer.Kind)}", 22f, 560f, RowHeight);
            UiFactory.Button(row, "Accept", 150f, RowHeight, () => Act(() => Diplomacy.Accept(offer, me)));
            UiFactory.Button(row, "Decline", 150f, RowHeight, () => Act(() => Diplomacy.Decline(offer, me)));
        }

        private void PlayerRow(Player me, Player other)
        {
            var row = UiFactory.Row(_playerRows, other.Name);
            var traitor = Diplomacy.IsTraitor(other) ? " <color=#FF5040>(traitor)</color>" : string.Empty;
            UiFactory.Label(row, Format.Name(other) + traitor, 22f, 240f, RowHeight);
            UiFactory.Label(row, Status(me, other), 22f, 300f, RowHeight);

            ProposeButton(row, me, other, TreatyKind.Pact, "Offer pact");
            ProposeButton(row, me, other, TreatyKind.Alliance, "Offer alliance");

            var treaty = Diplomacy.Between(me, other);
            if (treaty == null || treaty.BrokenBy != null)
                return;

            if (_confirmingBreak == other)
                UiFactory.Button(row, "Sure? Break", 170f, RowHeight, () =>
                {
                    _confirmingBreak = null;
                    Act(() => Diplomacy.Break(me, other));
                });
            else
                UiFactory.Button(row, "Break", 170f, RowHeight, () =>
                {
                    _confirmingBreak = other;
                    Refresh();
                });
        }

        private void ProposeButton(Transform row, Player me, Player other, TreatyKind kind, string label)
        {
            if (Diplomacy.HasOffered(me, other, kind))
                UiFactory.Label(row, "Offered", 22f, 170f, RowHeight, TextAlignmentOptions.Center);
            else if (Diplomacy.CanPropose(me, other, kind))
                UiFactory.Button(row, label, 170f, RowHeight, () => Act(() => Diplomacy.Propose(me, other, kind)));
        }

        private void Act(Func<bool> command)
        {
            if (command())
                _onChanged?.Invoke();
            Refresh();
        }

        private static string Status(Player me, Player other)
        {
            var treaty = Diplomacy.Between(me, other);
            if (treaty == null)
                return "At war";

            var status = treaty.Kind == TreatyKind.Alliance
                ? "Alliance"
                : $"Pact, {treaty.TurnsLeft} turn(s) left";
            return treaty.BrokenBy == null ? status : $"{status} - broken by {treaty.BrokenBy.Name}";
        }

        private static string Describe(TreatyKind kind) =>
            kind == TreatyKind.Pact ? $"a pact for {Diplomacy.PactTurns} turns" : "an alliance";
    }
}
