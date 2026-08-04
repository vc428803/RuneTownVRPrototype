using System.Collections.Generic;
using UnityEngine;

public class MonsterManager : MonoBehaviour
{
    // 修改原因：集中保存目前場景中實際存在的魔物，
    // 讓 Quest、掉落、存檔等系統不需要搜尋整個 Hierarchy。
    private readonly List<MonsterIdentity> activeMonsters =
        new List<MonsterIdentity>();

    // 修改原因：對外提供目前存活魔物數量，
    // 但不允許其他程式直接修改內部清單。
    public int ActiveMonsterCount =>
        activeMonsters.Count;

    public void RegisterMonster(MonsterIdentity monster)
    {
        if (monster == null || activeMonsters.Contains(monster))
        {
            return;
        }

        // 修改原因：魔物生成後由管理器統一登記，
        // 方便後續查詢目前場景中的存活魔物。
        activeMonsters.Add(monster);
    }

    public void UnregisterMonster(MonsterIdentity monster)
    {
        if (monster == null)
        {
            return;
        }

        // 修改原因：魔物死亡、離場或回收時移除登記，
        // 避免管理器保留已失效的物件參照。
        bool wasRemoved = activeMonsters.Remove(monster);

        if (!wasRemoved)
        {
            return;
        }

        // 修改原因：目前先透過 Log 驗證取消註冊流程，
        // 後續完成死亡系統後可再視需要移除。
        UnityEngine.Debug.Log(
            $"MonsterManager：{monster.DisplayName} 已取消註冊，" +
            $"目前登記數量為 {ActiveMonsterCount}。",
            this);
    }

}