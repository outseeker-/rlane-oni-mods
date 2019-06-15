﻿using KSerialization;
using System.Linq;
using System.Collections.Generic;

namespace Endpoint
{
    class EndpointTransport : KMonoBehaviour, ISidescreenButtonControl
    {
        [KSerialization.Serialize]
        public bool stay_at_destination;

        [KSerialization.Serialize]
        public bool has_reached_destination;

        public string SidescreenTitleKey => "STRINGS.UI.UISIDESCREENS.ENDPOINTTRANSPORT.TITLE";

        public string SidescreenStatusMessage
        {
            get
            {
                if (stay_at_destination)
                {
                    return "Duplicants will stay at the destination if it is habitable.";
                }
                else
                {
                    return "Duplicants will return with the rocket when the mission is complete.";
                }

            }
        }

        public string SidescreenButtonText
        {
            get
            {
                if (!stay_at_destination)
                {
                    return "STAY AT DESTINATION";
                }
                else
                {
                    return "RETURN TO COLONY";
                }
            }
        }

        public void OnSidescreenButtonPressed()
        {
            stay_at_destination = !stay_at_destination;
        }

        public void SetReachedDestination(bool reached_destination, SpaceDestination destination)
        {
            if (reached_destination != has_reached_destination)
            {
                has_reached_destination = reached_destination;
                if (reached_destination && destination.type == "Endpoint" && stay_at_destination)
                {
                    var storage = GetComponent<MinionStorage>();
                    var ids = storage.GetStoredMinionInfo().Select((x) => x.id).ToList();
                    var state = EndpointState.Load();
                    foreach (var id in ids)
                    {
                        var minion = storage.DeserializeMinion(id, transform.position);
                        var identity = minion.GetComponent<MinionIdentity>();
                        Debug.Log("Transported " + minion.name + " to " + destination.type);
                        minion.GetComponent<Schedulable>().GetSchedule().Unassign(minion.GetComponent<Schedulable>());
                        identity.GetSoleOwner().UnassignAll();
                        identity.GetEquipment().UnequipAll();
                        Components.MinionAssignablesProxy.Remove(identity.assignableProxy.Get());
                        Components.MinionResumes.Remove(minion.GetComponent<MinionResume>());
                        minion.gameObject.SetActive(false);
                        // Hacks to avoid crash in SkillsScreen.
                        Components.LiveMinionIdentities.Add(identity);
                        Components.LiveMinionIdentities.Remove(identity);
                        Game.Instance.userMenu.Refresh(gameObject);
                        // Record duplicant as rescued in the state file.
                        if (!state.times_rescued.ContainsKey(identity.name))
                        {
                            state.times_rescued[identity.name] = 0;
                        }
                        state.times_rescued[identity.name] += 1;
                    }
                    state.Save();
                }
            }
        }
    }
}
