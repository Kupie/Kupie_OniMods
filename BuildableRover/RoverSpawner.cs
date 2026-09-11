using UnityEngine;

namespace BuildableRover
{
	// Sits on the completed BuildableRover building. OnSpawn only fires once
	// the building has actually finished construction (the "under
	// construction" ghost is a different object) - so the moment this
	// runs, the 500kg of metal has already been delivered and consumed
	// by the normal Build chore, exactly like finishing any other
	// building.
	//
	// From here we just spawn a ScoutRover on the spot, colored the same
	// element the Depot was built from, and remove the Depot - it was
	// only ever a stand-in for "the Rover is being assembled here."
	public class RoverSpawner : KMonoBehaviour
	{
		private static readonly Tag ScoutRoverTag = "ScoutRover".ToTag();

		protected override void OnSpawn()
		{
			base.OnSpawn();

			// Deferred a tick rather than spawning + deleting inline:
			// OnSpawn can fire while sibling components on this same
			// object are still finishing their own OnSpawn/OnPrefabInit,
			// and DeleteObject() here would pull the rug out from under
			// them mid-initialization.
			GameScheduler.Instance.Schedule("BuildableRover.Spawn", 0f, SpawnRoverAndRemoveDepot);
		}

		private void SpawnRoverAndRemoveDepot(object data)
		{
			if (this == null || gameObject == null)
			{
				return;
			}

			// Whatever raw metal the Duplicant actually built this out of
			// (Copper, Iron, whatever was available/chosen) - matches how
			// the vanilla Lander+Rover inherit the module's material.
			PrimaryElement depotElement = GetComponent<PrimaryElement>();
			SimHashes element = (depotElement != null) ? depotElement.ElementID : SimHashes.Copper;

			Vector3 pos = transform.GetPosition();
			GameObject rover = GameUtil.KInstantiate(Assets.GetPrefab(ScoutRoverTag), pos, Grid.SceneLayer.Creatures, null, 0);

			PrimaryElement roverElement = rover.GetComponent<PrimaryElement>();
			if (roverElement != null)
			{
				roverElement.SetElement(element, false);
			}

			rover.SetActive(true);

			// Same bookkeeping a landed Rover gets in CargoDropperStorage -
			// mostly just flags this world as "a Rover has been here" for
			// anything (codex, story traits) that checks for it.
			WorldContainer world = rover.GetMyWorld();
			if (world != null)
			{
				world.SetRoverLanded();
			}

			gameObject.DeleteObject();
		}
	}
}
