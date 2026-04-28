using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;

namespace TurnBasedBattle
{
    /// <summary>
    /// 画面上やコンソールへの簡易的なバトルログ出力を担当する。
    /// </summary>
    public class SimpleBattleLogPrinter : MonoBehaviour
    {
        [SerializeField] private BattleManager battleManager;
        [SerializeField] private TMP_Text targetText;
        [SerializeField, Min(1)] private int maxLines = 12;

        private readonly Queue<string> lines = new Queue<string>();

        private void OnEnable()
        {
            if (battleManager != null)
            {
                battleManager.OnBattleLog += HandleLog;
            }
        }

        private void OnDisable()
        {
            if (battleManager != null)
            {
                battleManager.OnBattleLog -= HandleLog;
            }
        }

        private void HandleLog(string message)
        {
            if (targetText == null)
            {
                return;
            }

            lines.Enqueue(message);
            while (lines.Count > maxLines)
            {
                lines.Dequeue();
            }

            StringBuilder sb = new StringBuilder();
            foreach (string line in lines)
            {
                sb.AppendLine(line);
            }
            targetText.text = sb.ToString();
        }
    }
}
