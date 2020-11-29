using System.Collections;
using System.Collections.Generic;
using Runtime.GameSystem;
using Runtime.Utils;
using UnityEngine;

namespace Runtime.ItemSystem
{
	public class ItemManager : Singleton<ItemManager>
	{
		#region Serialize Fields

		[SerializeField,] private float _itemSpawnChance = 0.7f;
		[SerializeField,] private float _doubleItemSpawnChance = 0.1f;
		[SerializeField,] private float _itemSpawnDelay = 3f;
		[SerializeField,] private float _itemSpawnDelayRandom = 1f;
		[SerializeField,] private List<ItemBase> _possibleItemPrefabs;

		#endregion

		#region Unity methods

		private IEnumerator Start()
		{
			while (GameManager.Instance.State != GameState.Active)
			{
				yield return null;
			}

			StartCoroutine(SpawnItemsCoroutine());
		}

		#endregion

		#region Private methods

		private IEnumerator SpawnItemsCoroutine()
		{
			float randomValue = Random.value;
			if (randomValue > _itemSpawnChance)
			{
				yield break;
			}

			yield return new WaitForSeconds(_itemSpawnDelay + (Random.value * _itemSpawnDelayRandom));

			SpawnRandomItem();
		}

		private void SpawnRandomItem()
		{
			var itemPrefab = _possibleItemPrefabs[Random.Range(0, _possibleItemPrefabs.Count)];

			Vector3 position;
			do
			{
				position = (Vector2.one * 5) + (Random.insideUnitCircle * 4.5f);
			} while (GameManager.Instance.Players.TrueForAll(player => Vector3.Distance(player.transform.position, position) > 5));

			Instantiate(itemPrefab, position, Quaternion.identity);
		}

		#endregion
	}
}