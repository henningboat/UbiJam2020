using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Runtime.GameSystem;
using Runtime.Utils;
using UnityEngine;

namespace Runtime.ItemSystem
{
	[RequireComponent(typeof(PhotonView))]
	public class ItemManager : Singleton<ItemManager>
	{
		#region Serialize Fields

		[SerializeField,] private float _itemSpawnChance = 0.7f;
		[SerializeField,] private float _doubleItemSpawnChance = 0.1f;
		[SerializeField,] private float _itemSpawnDelay = 3f;
		[SerializeField,] private float _itemSpawnDelayRandom = 1f;
		[SerializeField] private int _startSpawningItemsAfterRounds = 3;
		[SerializeField,] private List<ItemBase> _possibleItemPrefabs;
		[SerializeField,] private ItemBase _debugItem;
		private PhotonView _photonView;

		#endregion

		#region Unity methods

		private IEnumerator Start()
		{
			_photonView = GetComponent<PhotonView>();
			if (!_photonView.IsMine)
			{
				yield break;
			}

			while (GameManager.Instance.State != GameState.Active)
			{
				yield return null;
			}

			if (_debugItem != null)
			{
				_itemSpawnChance = 1;
			}
			
			if (GameManager.RoundCount > _startSpawningItemsAfterRounds || _debugItem != null)
			{
				StartCoroutine(SpawnItemsCoroutine());
			}
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

			if (randomValue < _doubleItemSpawnChance)
			{
				yield return new WaitForSeconds(_itemSpawnDelay + (Random.value * _itemSpawnDelayRandom));
				SpawnRandomItem();
			}
		}

		private void SpawnRandomItem()
		{
			ItemBase itemPrefab;
			if ((_debugItem != null) && Application.isEditor)
			{
				itemPrefab = _debugItem;
			}
			else
			{
				int randomItemIndex = Random.Range(0, _possibleItemPrefabs.Count);
				itemPrefab = _possibleItemPrefabs[randomItemIndex];
				_possibleItemPrefabs.RemoveAt(randomItemIndex);
			}

			Vector3 position = (Vector2.one * 5) + (Random.insideUnitCircle * 4.5f);

			int count = 0;
			while (count < 500 && GameManager.Instance.Players.Any(player => Vector3.Distance(player.transform.position, position) < 2))
			{
				count++;
				position = (Vector2.one * 5) + (Random.insideUnitCircle * 4.5f);
			}

			PhotonNetwork.Instantiate(itemPrefab.name, position, Quaternion.identity);
		}

		#endregion
	}
}