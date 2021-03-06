using System;
using System.Reflection;
using ColossalFramework;
using ICities;
using TrafficManager.Custom.AI;
using TrafficManager.Geometry;
using TrafficManager.TrafficLight;
using TrafficManager.UI;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Collections.Generic;
using TrafficManager.State;
using ColossalFramework.UI;
using ColossalFramework.Math;
using TrafficManager.Custom.PathFinding;
using TrafficManager.Util;
using TrafficManager.Custom.Manager;
using System.Linq;
using TrafficManager.Manager;

namespace TrafficManager {
    public class LoadingExtension : LoadingExtensionBase {
		public class Detour {
			public MethodInfo OriginalMethod;
			public MethodInfo CustomMethod;
			public RedirectCallsState Redirect;

			public Detour(MethodInfo originalMethod, MethodInfo customMethod) {
				this.OriginalMethod = originalMethod;
				this.CustomMethod = customMethod;
				this.Redirect = RedirectionHelper.RedirectCalls(originalMethod, customMethod);
			}
		}

        public static LoadingExtension Instance;
#if !TAM
		public static bool IsPathManagerCompatible {
			get; private set;
		} = true;
#endif
		public static bool IsPathManagerReplaced {
			get; private set;
		} = false;
		public static bool IsRainfallLoaded {
			get; private set;
		} = false;
		public CustomPathManager CustomPathManager { get; set; }
        public static bool DetourInited { get; set; }
		public static List<Detour> Detours { get; set; }
        public TrafficManagerMode ToolMode { get; set; }
        public TrafficManagerTool TrafficManagerTool { get; set; }
#if !TAM
		public UIBase UI { get; set; }
#endif

		private static bool gameLoaded = false;

        public LoadingExtension() {
        }

		public void revertDetours() {
			if (DetourInited) {
				Log.Info("Revert detours");
				Detours.Reverse();
				foreach (Detour d in Detours) {
					RedirectionHelper.RevertRedirect(d.OriginalMethod, d.Redirect);
				}
				DetourInited = false;
				Detours.Clear();
			}
		}

		public void initDetours() {
			if (!DetourInited) {
				Log.Info("Init detours");
				bool detourFailed = false;

				// REVERSE REDIRECTION

				Log.Info("Reverse-Redirection CustomVehicleManager::ReleaseVehicleImplementation calls");
				try {
					Detours.Add(new Detour(typeof(CustomVehicleManager).GetMethod("ReleaseVehicleImplementation",
							BindingFlags.NonPublic | BindingFlags.Instance,
							null,
							new[]
							{
									typeof (ushort),
									typeof (Vehicle).MakeByRefType(),
							},
							null),
							typeof(VehicleManager).GetMethod("ReleaseVehicleImplementation",
								BindingFlags.NonPublic | BindingFlags.Instance,
								null,
								new[]
								{
									typeof (ushort),
									typeof (Vehicle).MakeByRefType(),
								},
								null)));
				} catch (Exception) {
					Log.Error("Could not reverse-redirect CustomVehicleManager::ReleaseVehicleImplementation");
					detourFailed = true;
				}

				Log.Info("Reverse-Redirection CarAI::CheckOverlap calls");
				try {
					Detours.Add(new Detour(typeof(CustomCarAI).GetMethod("CheckOverlap",
							BindingFlags.NonPublic | BindingFlags.Static,
							null,
							new[]
							{
									typeof (Segment3),
									typeof (ushort),
									typeof (float),
							},
							null),
							typeof(CarAI).GetMethod("CheckOverlap",
								BindingFlags.NonPublic | BindingFlags.Static,
								null,
								new[]
								{
									typeof (Segment3),
									typeof (ushort),
									typeof (float),
								},
								null)));
				} catch (Exception) {
					Log.Error("Could not reverse-redirect CarAI::CheckOverlap");
					detourFailed = true;
				}

				Log.Info("Reverse-Redirection TrainAI::InitializePath calls");
				try {
					Detours.Add(new Detour(typeof(CustomTrainAI).GetMethod("InitializePath",
							BindingFlags.NonPublic | BindingFlags.Static,
							null,
							new[]
							{
									typeof (ushort),
									typeof (Vehicle).MakeByRefType()
							},
							null),
							typeof(TrainAI).GetMethod("InitializePath",
								BindingFlags.NonPublic | BindingFlags.Static,
								null,
								new[]
								{
									typeof (ushort),
									typeof (Vehicle).MakeByRefType()
								},
								null)));
				} catch (Exception) {
					Log.Error("Could not reverse-redirect TrainAI::InitializePath");
					detourFailed = true;
				}

				Log.Info("Reverse-Redirection TramBaseAI::InitializePath calls");
				try {
					Detours.Add(new Detour(typeof(CustomTramBaseAI).GetMethod("InitializePath",
							BindingFlags.NonPublic | BindingFlags.Static,
							null,
							new[]
							{
									typeof (ushort),
									typeof (Vehicle).MakeByRefType()
							},
							null),
							typeof(TramBaseAI).GetMethod("InitializePath",
								BindingFlags.NonPublic | BindingFlags.Static,
								null,
								new[]
								{
									typeof (ushort),
									typeof (Vehicle).MakeByRefType()
								},
								null)));
				} catch (Exception) {
					Log.Error("Could not reverse-redirect TramBaseAI::InitializePath");
					detourFailed = true;
				}

				Log.Info("Reverse-Redirection CustomRoadAI::CheckBuildings calls");
				try {
					Detours.Add(new Detour(typeof(CustomRoadAI).GetMethod("CheckBuildings",
							BindingFlags.NonPublic | BindingFlags.Instance,
							null,
							new[]
							{
									typeof (ushort),
									typeof (NetSegment).MakeByRefType(),
							},
							null),
							typeof(RoadBaseAI).GetMethod("CheckBuildings",
								BindingFlags.NonPublic | BindingFlags.Instance,
								null,
								new[]
								{
									typeof (ushort),
									typeof (NetSegment).MakeByRefType(),
								},
								null)));
				} catch (Exception) {
					Log.Error("Could not reverse-redirect CustomRoadAI::CheckBuildings");
					detourFailed = true;
				}

				Log.Info("Reverse-Redirection CustomTrainAI::CheckOverlap calls (1)");
				try {
					Detours.Add(new Detour(typeof(CustomTrainAI).GetMethod("CheckOverlap",
							BindingFlags.NonPublic | BindingFlags.Static,
							null,
							new[]
							{
									typeof (ushort),
									typeof (Vehicle).MakeByRefType(),
									typeof (Segment3),
									typeof (ushort)
							},
							null),
							typeof(TrainAI).GetMethod("CheckOverlap",
								BindingFlags.NonPublic | BindingFlags.Static,
								null,
								new[]
								{
									typeof (ushort),
									typeof (Vehicle).MakeByRefType(),
									typeof (Segment3),
									typeof (ushort)
								},
								null)));
				} catch (Exception) {
					Log.Error("Could not reverse-redirect CustomRoadBaseAI::CheckOverlap (1)");
					detourFailed = true;
				}

				Log.Info("Reverse-Redirection CustomTrainAI::CheckOverlap calls (2)");
				try {
					Detours.Add(new Detour(typeof(CustomTrainAI).GetMethod("CheckOverlap",
							BindingFlags.NonPublic | BindingFlags.Static,
							null,
							new[]
							{
									typeof (ushort),
									typeof (Vehicle).MakeByRefType(),
									typeof (Segment3),
									typeof (ushort),
									typeof (ushort),
									typeof (Vehicle).MakeByRefType(),
									typeof (bool).MakeByRefType(),
									typeof (Vector3),
									typeof (Vector3)
							},
							null), typeof(TrainAI).GetMethod("CheckOverlap",
							BindingFlags.NonPublic | BindingFlags.Static,
							null,
							new[]
							{
									typeof (ushort),
									typeof (Vehicle).MakeByRefType(),
									typeof (Segment3),
									typeof (ushort),
									typeof (ushort),
									typeof (Vehicle).MakeByRefType(),
									typeof (bool).MakeByRefType(),
									typeof (Vector3),
									typeof (Vector3)
							},
							null)));
				} catch (Exception) {
					Log.Error("Could not reverse-redirect CustomRoadBaseAI::CheckOverlap (2)");
					detourFailed = true;
				}

				// FORWARD REDIRECTION

				Log.Info("Redirecting Vehicle AI Calculate Segment Calls (1)");
				try {
					Detours.Add(new Detour(typeof(VehicleAI).GetMethod("CalculateSegmentPosition",
							BindingFlags.NonPublic | BindingFlags.Instance,
							null,
							new[]
							{
								typeof (ushort),
								typeof (Vehicle).MakeByRefType(),
								typeof (PathUnit.Position),
								typeof (PathUnit.Position),
								typeof (uint),
								typeof (byte),
								typeof (PathUnit.Position),
								typeof (uint),
								typeof (byte),
								typeof (int),
								typeof (Vector3).MakeByRefType(),
								typeof (Vector3).MakeByRefType(),
								typeof (float).MakeByRefType()
							},
							null),
							typeof(CustomVehicleAI).GetMethod("CustomCalculateSegmentPosition")));
				} catch (Exception) {
					Log.Error("Could not redirect VehicleAI::CalculateSegmentPosition (1).");
					detourFailed = true;
				}


				Log.Info("Redirecting Vehicle AI Calculate Segment Calls (2)");
				try {
					Detours.Add(new Detour(typeof(VehicleAI).GetMethod("CalculateSegmentPosition",
							BindingFlags.NonPublic | BindingFlags.Instance,
							null,
							new[]
							{
								typeof (ushort),
								typeof (Vehicle).MakeByRefType(),
								typeof (PathUnit.Position),
								typeof (uint),
								typeof (byte),
								typeof (Vector3).MakeByRefType(),
								typeof (Vector3).MakeByRefType(),
								typeof (float).MakeByRefType()
							},
							null),
							typeof(CustomVehicleAI).GetMethod("CustomCalculateSegmentPositionPathFinder")));
				} catch (Exception) {
					Log.Error("Could not redirect VehicleAI::CalculateSegmentPosition (2).");
					detourFailed = true;
				}

				Log.Info("Redirection VehicleManager::ReleaseVehicle calls");
				try {
					Detours.Add(new Detour(typeof(VehicleManager).GetMethod("ReleaseVehicle",
							BindingFlags.Public | BindingFlags.Instance,
							null,
							new[]
							{
									typeof (ushort)
							},
							null),
							typeof(CustomVehicleManager).GetMethod("CustomReleaseVehicle")));
				} catch (Exception) {
					Log.Error("Could not redirect VehicleManager::ReleaseVehicle");
					detourFailed = true;
				}

				Log.Info("Redirection VehicleManager::CreateVehicle calls");
				try {
					Detours.Add(new Detour(typeof(VehicleManager).GetMethod("CreateVehicle",
							BindingFlags.Public | BindingFlags.Instance,
							null,
							new[]
							{
									typeof (ushort).MakeByRefType(),
									typeof (Randomizer).MakeByRefType(),
									typeof (VehicleInfo),
									typeof (Vector3),
									typeof (TransferManager.TransferReason),
									typeof (bool),
									typeof (bool)
							},
							null),
							typeof(CustomVehicleManager).GetMethod("CustomCreateVehicle")));
				} catch (Exception) {
					Log.Error("Could not redirect VehicleManager::CreateVehicle calls");
					detourFailed = true;
				}
			
				Log.Info("Redirecting TramBaseAI Calculate Segment Calls (2)");
				try {
					Detours.Add(new Detour(typeof(TramBaseAI).GetMethod("CalculateSegmentPosition",
							BindingFlags.NonPublic | BindingFlags.Instance,
							null,
							new[]
							{
								typeof (ushort),
								typeof (Vehicle).MakeByRefType(),
								typeof (PathUnit.Position),
								typeof (uint),
								typeof (byte),
								typeof (Vector3).MakeByRefType(),
								typeof (Vector3).MakeByRefType(),
								typeof (float).MakeByRefType()
							},
							null),
							typeof(CustomTramBaseAI).GetMethod("CustomCalculateSegmentPositionPathFinder")));
				} catch (Exception) {
					Log.Error("Could not redirect TramBaseAI::CalculateSegmentPosition (2).");
					detourFailed = true;
				}

				Log.Info("Redirecting RoadBaseAI.SimulationStep for nodes");
				try {
					Detours.Add(new Detour(typeof(RoadBaseAI).GetMethod("SimulationStep", new[] { typeof(ushort), typeof(NetNode).MakeByRefType() }),
						typeof(CustomRoadAI).GetMethod("CustomNodeSimulationStep")));
				} catch (Exception) {
					Log.Error("Could not redirect RoadBaseAI::SimulationStep.");
					detourFailed = true;
				}

				Log.Info("Redirecting RoadBaseAI.SimulationStep for segments");
				try {
					Detours.Add(new Detour(typeof(RoadBaseAI).GetMethod("SimulationStep", new[] { typeof(ushort), typeof(NetSegment).MakeByRefType() }),
						typeof(CustomRoadAI).GetMethod("CustomSegmentSimulationStep")));
				} catch (Exception) {
					Log.Error("Could not redirect RoadBaseAI::SimulationStep.");
				}

				Log.Info("Redirecting Human AI Calls");
				try {
					Detours.Add(new Detour(typeof(HumanAI).GetMethod("CheckTrafficLights",
							BindingFlags.NonPublic | BindingFlags.Instance,
							null,
							new[] { typeof(ushort), typeof(ushort) },
							null),
							typeof(CustomHumanAI).GetMethod("CustomCheckTrafficLights")));
				} catch (Exception) {
					Log.Error("Could not redirect HumanAI::CheckTrafficLights.");
					detourFailed = true;
				}

				Log.Info("Redirecting CarAI::TrySpawn Calls");
				try {
					Detours.Add(new Detour(typeof(CarAI).GetMethod("TrySpawn",
								new[] {
									typeof (ushort),
									typeof (Vehicle).MakeByRefType()
								}),
								typeof(CustomCarAI).GetMethod("TrySpawn")));
				} catch (Exception) {
					Log.Error("Could not redirect CarAI::TrySpawn.");
					detourFailed = true;
				}

				Log.Info("Redirecting CarAI Simulation Step Calls");
				try {
					Detours.Add(new Detour(typeof(CarAI).GetMethod("SimulationStep",
								new[] {
									typeof (ushort),
									typeof (Vehicle).MakeByRefType(),
									typeof (Vector3)
								}),
								typeof(CustomCarAI).GetMethod("CustomSimulationStep")));
				} catch (Exception) {
					Log.Error("Could not redirect CarAI::SimulationStep.");
					detourFailed = true;
				}

				Log.Info("Redirecting PassengerCarAI Simulation Step Calls");
				try {
					Detours.Add(new Detour(typeof(PassengerCarAI).GetMethod("SimulationStep",
							new[] { typeof(ushort), typeof(Vehicle).MakeByRefType(), typeof(Vector3) }),
							typeof(CustomPassengerCarAI).GetMethod("CustomSimulationStep")));
				} catch (Exception) {
					Log.Error("Could not redirect PassengerCarAI::SimulationStep.");
					detourFailed = true;
				}

				Log.Info("Redirecting CargoTruckAI Simulation Step Calls");
				try {
					Detours.Add(new Detour(typeof(CargoTruckAI).GetMethod("SimulationStep",
								new[] { typeof(ushort), typeof(Vehicle).MakeByRefType(), typeof(Vector3) }),
								typeof(CustomCargoTruckAI).GetMethod("CustomSimulationStep")));
				} catch (Exception) {
					Log.Error("Could not redirect CargoTruckAI::SimulationStep.");
					detourFailed = true;
				}

				Log.Info("Redirecting TrainAI Simulation Step Calls");
				try {
					Detours.Add(new Detour(typeof(TrainAI).GetMethod("SimulationStep",
								new[] {
									typeof (ushort),
									typeof (Vehicle).MakeByRefType(),
									typeof (Vector3)
								}),
								typeof(CustomTrainAI).GetMethod("TrafficManagerSimulationStep")));
				} catch (Exception) {
					Log.Error("Could not redirect TrainAI::SimulationStep.");
					detourFailed = true;
				}

				Log.Info("Redirecting TrainAI::TrySpawn Calls");
				try {
					Detours.Add(new Detour(typeof(TrainAI).GetMethod("TrySpawn",
								new[] {
									typeof (ushort),
									typeof (Vehicle).MakeByRefType()
								}),
								typeof(CustomTrainAI).GetMethod("TrySpawn")));
				} catch (Exception) {
					Log.Error("Could not redirect TrainAI::TrySpawn.");
					detourFailed = true;
				}

				Log.Info("Redirection TramBaseAI::SimulationStep calls");
				try {
					Detours.Add(new Detour(typeof(TramBaseAI).GetMethod("SimulationStep",
							BindingFlags.Public | BindingFlags.Instance,
							null,
							new[]
							{
								typeof (ushort),
								typeof (Vehicle).MakeByRefType(),
								typeof (Vector3),
							},
							null), typeof(CustomTramBaseAI).GetMethod("CustomSimulationStep")));
				} catch (Exception) {
					Log.Error("Could not redirect TramBaseAI::SimulationStep");
					detourFailed = true;
				}

				Log.Info("Redirecting TramBaseAI::TrySpawn Calls");
				try {
					Detours.Add(new Detour(typeof(TramBaseAI).GetMethod("TrySpawn",
								new[] {
									typeof (ushort),
									typeof (Vehicle).MakeByRefType()
								}),
								typeof(CustomTramBaseAI).GetMethod("TrySpawn")));
				} catch (Exception) {
					Log.Error("Could not redirect TramBaseAI::TrySpawn.");
					detourFailed = true;
				}

				Log.Info("Redirecting Car AI Calculate Segment Calls");
				try {
					Detours.Add(new Detour(typeof(CarAI).GetMethod("CalculateSegmentPosition",
							BindingFlags.NonPublic | BindingFlags.Instance,
							null,
							new[]
							{
								typeof (ushort),
								typeof (Vehicle).MakeByRefType(),
								typeof (PathUnit.Position),
								typeof (PathUnit.Position),
								typeof (uint),
								typeof (byte),
								typeof (PathUnit.Position),
								typeof (uint),
								typeof (byte),
								typeof (int),
								typeof (Vector3).MakeByRefType(),
								typeof (Vector3).MakeByRefType(),
								typeof (float).MakeByRefType()
							},
							null),
							typeof(CustomCarAI).GetMethod("CustomCalculateSegmentPosition")));
				} catch (Exception) {
					Log.Error("Could not redirect CarAI::CalculateSegmentPosition.");
					detourFailed = true;
				}

				Log.Info("Redirection TramBaseAI Calculate Segment Position calls");
				try {
					Detours.Add(new Detour(typeof(TramBaseAI).GetMethod("CalculateSegmentPosition",
							BindingFlags.NonPublic | BindingFlags.Instance,
							null,
							new[]
							{
									typeof (ushort),
									typeof (Vehicle).MakeByRefType(),
									typeof (PathUnit.Position),
									typeof (PathUnit.Position),
									typeof (uint),
									typeof (byte),
									typeof (PathUnit.Position),
									typeof (uint),
									typeof (byte),
									typeof (int),
									typeof (Vector3).MakeByRefType(),
									typeof (Vector3).MakeByRefType(),
									typeof (float).MakeByRefType()
							},
							null),
							typeof(CustomTramBaseAI).GetMethod("CustomCalculateSegmentPosition")));
				} catch (Exception) {
					Log.Error("Could not redirect TramBaseAI::CalculateSegmentPosition");
					detourFailed = true;
				}

				if (IsPathManagerCompatible) {
					Log.Info("Redirection PathFind::CalculatePath calls for non-Traffic++");
					try {
						Detours.Add(new Detour(typeof(PathFind).GetMethod("CalculatePath",
								BindingFlags.Public | BindingFlags.Instance,
								null,
								new[]
								{
									typeof (uint),
									typeof (bool)
								},
								null),
								typeof(CustomPathFind).GetMethod("CalculatePath")));
					} catch (Exception) {
						Log.Error("Could not redirect PathFind::CalculatePath");
						detourFailed = true;
					}

					Log.Info("Redirection PathManager::ReleasePath calls for non-Traffic++");
					try {
						Detours.Add(new Detour(typeof(PathManager).GetMethod("ReleasePath",
								BindingFlags.Public | BindingFlags.Instance,
								null,
								new[]
								{
									typeof (uint)
								},
								null),
								typeof(CustomPathManager).GetMethod("ReleasePath")));
					} catch (Exception) {
						Log.Error("Could not redirect PathManager::ReleasePath");
						detourFailed = true;
					}

					Log.Info("Redirection CarAI Calculate Segment Position calls for non-Traffic++");
					try {
						Detours.Add(new Detour(typeof(CarAI).GetMethod("CalculateSegmentPosition",
								BindingFlags.NonPublic | BindingFlags.Instance,
								null,
								new[]
								{
									typeof (ushort),
									typeof (Vehicle).MakeByRefType(),
									typeof (PathUnit.Position),
									typeof (uint),
									typeof (byte),
									typeof (Vector3).MakeByRefType(),
									typeof (Vector3).MakeByRefType(),
									typeof (float).MakeByRefType()
								},
								null),
								typeof(CustomCarAI).GetMethod("CustomCalculateSegmentPositionPathFinder")));
					} catch (Exception) {
						Log.Error("Could not redirect CarAI::CalculateSegmentPosition");
						detourFailed = true;
					}

					Log.Info("Redirection TrainAI Calculate Segment Position calls for non-Traffic++");
					try {
						Detours.Add(new Detour(typeof(TrainAI).GetMethod("CalculateSegmentPosition",
								BindingFlags.NonPublic | BindingFlags.Instance,
								null,
								new[]
								{
									typeof (ushort),
									typeof (Vehicle).MakeByRefType(),
									typeof (PathUnit.Position),
									typeof (uint),
									typeof (byte),
									typeof (Vector3).MakeByRefType(),
									typeof (Vector3).MakeByRefType(),
									typeof (float).MakeByRefType()
								},
								null),
								typeof(CustomTrainAI).GetMethod("TmCalculateSegmentPositionPathFinder")));
					} catch (Exception) {
						Log.Error("Could not redirect TrainAI::CalculateSegmentPosition (2)");
						detourFailed = true;
					}

					Log.Info("Redirection AmbulanceAI::StartPathFind calls");
					try {
						Detours.Add(new Detour(typeof(AmbulanceAI).GetMethod("StartPathFind",
								BindingFlags.NonPublic | BindingFlags.Instance,
								null,
								new[]
								{
									typeof (ushort),
									typeof (Vehicle).MakeByRefType(),
									typeof (Vector3),
									typeof (Vector3),
									typeof (bool),
									typeof (bool),
									typeof (bool)
								},
								null),
								typeof(CustomAmbulanceAI).GetMethod("CustomStartPathFind")));
					} catch (Exception) {
						Log.Error("Could not redirect AmbulanceAI::StartPathFind");
						detourFailed = true;
					}

					Log.Info("Redirection BusAI::StartPathFind calls");
					try {
						Detours.Add(new Detour(typeof(BusAI).GetMethod("StartPathFind",
								BindingFlags.NonPublic | BindingFlags.Instance,
								null,
								new[]
								{
									typeof (ushort),
									typeof (Vehicle).MakeByRefType(),
									typeof (Vector3),
									typeof (Vector3),
									typeof (bool),
									typeof (bool),
									typeof (bool)
								},
								null),
								typeof(CustomBusAI).GetMethod("CustomStartPathFind")));
					} catch (Exception) {
						Log.Error("Could not redirect BusAI::StartPathFind");
						detourFailed = true;
					}

					Log.Info("Redirection CarAI::StartPathFind calls");
					try {
						Detours.Add(new Detour(typeof(CarAI).GetMethod("StartPathFind",
								BindingFlags.NonPublic | BindingFlags.Instance,
								null,
								new[]
								{
									typeof (ushort),
									typeof (Vehicle).MakeByRefType(),
									typeof (Vector3),
									typeof (Vector3),
									typeof (bool),
									typeof (bool),
									typeof (bool)
								},
								null),
								typeof(CustomCarAI).GetMethod("CustomStartPathFind")));
					} catch (Exception) {
						Log.Error("Could not redirect CarAI::StartPathFind");
						detourFailed = true;
					}

					Log.Info("Redirection CargoTruckAI::StartPathFind calls");
					try {
						Detours.Add(new Detour(typeof(CargoTruckAI).GetMethod("StartPathFind",
								BindingFlags.NonPublic | BindingFlags.Instance,
								null,
								new[]
								{
									typeof (ushort),
									typeof (Vehicle).MakeByRefType(),
									typeof (Vector3),
									typeof (Vector3),
									typeof (bool),
									typeof (bool),
									typeof (bool)
								},
								null),
								typeof(CustomCargoTruckAI).GetMethod("CustomStartPathFind")));
					} catch (Exception) {
						Log.Error("Could not redirect CargoTruckAI::StartPathFind");
						detourFailed = true;
					}

					Log.Info("Redirection FireTruckAI::StartPathFind calls");
					try {
						Detours.Add(new Detour(typeof(FireTruckAI).GetMethod("StartPathFind",
								BindingFlags.NonPublic | BindingFlags.Instance,
								null,
								new[]
								{
									typeof (ushort),
									typeof (Vehicle).MakeByRefType(),
									typeof (Vector3),
									typeof (Vector3),
									typeof (bool),
									typeof (bool),
									typeof (bool)
								},
								null),
								typeof(CustomFireTruckAI).GetMethod("CustomStartPathFind")));
					} catch (Exception) {
						Log.Error("Could not redirect FireTruckAI::StartPathFind");
						detourFailed = true;
					}

					Log.Info("Redirection PassengerCarAI::StartPathFind calls");
					try {
						Detours.Add(new Detour(typeof(PassengerCarAI).GetMethod("StartPathFind",
								BindingFlags.NonPublic | BindingFlags.Instance,
								null,
								new[]
								{
									typeof (ushort),
									typeof (Vehicle).MakeByRefType(),
									typeof (Vector3),
									typeof (Vector3),
									typeof (bool),
									typeof (bool),
									typeof (bool)
								},
								null),
								typeof(CustomPassengerCarAI).GetMethod("CustomStartPathFind")));
					} catch (Exception) {
						Log.Error("Could not redirect PassengerCarAI::StartPathFind");
						detourFailed = true;
					}

					Log.Info("Redirection PoliceCarAI::StartPathFind calls");
					try {
						Detours.Add(new Detour(typeof(PoliceCarAI).GetMethod("StartPathFind",
								BindingFlags.NonPublic | BindingFlags.Instance,
								null,
								new[]
								{
									typeof (ushort),
									typeof (Vehicle).MakeByRefType(),
									typeof (Vector3),
									typeof (Vector3),
									typeof (bool),
									typeof (bool),
									typeof (bool)
								},
								null),
								typeof(CustomPoliceCarAI).GetMethod("CustomStartPathFind")));
					} catch (Exception) {
						Log.Error("Could not redirect PoliceCarAI::StartPathFind");
						detourFailed = true;
					}

					Log.Info("Redirection TaxiAI::StartPathFind calls");
					try {
						Detours.Add(new Detour(typeof(TaxiAI).GetMethod("StartPathFind",
								BindingFlags.NonPublic | BindingFlags.Instance,
								null,
								new[]
								{
									typeof (ushort),
									typeof (Vehicle).MakeByRefType(),
									typeof (Vector3),
									typeof (Vector3),
									typeof (bool),
									typeof (bool),
									typeof (bool)
								},
								null),
								typeof(CustomTaxiAI).GetMethod("CustomStartPathFind")));
					} catch (Exception) {
						Log.Error("Could not redirect TaxiAI::StartPathFind");
						detourFailed = true;
					}

					Log.Info("Redirection TrainAI::StartPathFind calls");
					try {
						Detours.Add(new Detour(typeof(TrainAI).GetMethod("StartPathFind",
								BindingFlags.NonPublic | BindingFlags.Instance,
								null,
								new[]
								{
									typeof (ushort),
									typeof (Vehicle).MakeByRefType(),
									typeof (Vector3),
									typeof (Vector3),
									typeof (bool),
									typeof (bool)
								},
								null),
								typeof(CustomTrainAI).GetMethod("CustomStartPathFind")));
					} catch (Exception) {
						Log.Error("Could not redirect TrainAI::StartPathFind");
						detourFailed = true;
					}

					Log.Info("Redirection ShipAI::StartPathFind calls");
					try {
						Detours.Add(new Detour(typeof(ShipAI).GetMethod("StartPathFind",
								BindingFlags.NonPublic | BindingFlags.Instance,
								null,
								new[]
								{
									typeof (ushort),
									typeof (Vehicle).MakeByRefType(),
									typeof (Vector3),
									typeof (Vector3),
									typeof (bool),
									typeof (bool)
								},
								null),
								typeof(CustomShipAI).GetMethod("CustomStartPathFind")));
					} catch (Exception) {
						Log.Error("Could not redirect ShipAI::StartPathFind");
						detourFailed = true;
					}

					Log.Info("Redirection CitizenAI::StartPathFind calls");
					try {
						Detours.Add(new Detour(typeof(CitizenAI).GetMethod("StartPathFind",
								BindingFlags.NonPublic | BindingFlags.Instance,
								null,
								new[]
								{
									typeof (ushort),
									typeof (CitizenInstance).MakeByRefType(),
									typeof (Vector3),
									typeof (Vector3),
									typeof (VehicleInfo)
								},
								null),
								typeof(CustomCitizenAI).GetMethod("CustomStartPathFind")));
					} catch (Exception) {
						Log.Error("Could not redirect CitizenAI::StartPathFind");
						detourFailed = true;
					}

					Log.Info("Redirection TransportLineAI::StartPathFind calls");
					try {
						Detours.Add(new Detour(typeof(TransportLineAI).GetMethod("StartPathFind",
								BindingFlags.Public | BindingFlags.Static,
								null,
								new[]
								{
									typeof (ushort),
									typeof (NetSegment).MakeByRefType(),
									typeof (ItemClass.Service),
									typeof (VehicleInfo.VehicleType),
									typeof (bool)
								},
								null),
								typeof(CustomTransportLineAI).GetMethod("CustomStartPathFind")));
					} catch (Exception) {
						Log.Error("Could not redirect TransportLineAI::StartPathFind");
						detourFailed = true;
					}

					Log.Info("Redirection TramBaseAI::StartPathFind calls");
					try {
						Detours.Add(new Detour(typeof(TramBaseAI).GetMethod("StartPathFind",
								BindingFlags.NonPublic | BindingFlags.Instance,
								null,
								new[]
								{
									typeof (ushort),
									typeof (Vehicle).MakeByRefType(),
									typeof (Vector3),
									typeof (Vector3),
									typeof (bool),
									typeof (bool)
								},
								null),
								typeof(CustomTramBaseAI).GetMethod("CustomStartPathFind")));
					} catch (Exception) {
						Log.Error("Could not redirect TramBaseAI::StartPathFind");
						detourFailed = true;
					}
				}

				Log.Info("Redirection RoadBaseAI::SetTrafficLightState calls");
				try {
					Detours.Add(new Detour(typeof(RoadBaseAI).GetMethod("SetTrafficLightState",
							BindingFlags.Public | BindingFlags.Static,
							null,
							new[]
							{
									typeof (ushort),
									typeof (NetSegment).MakeByRefType(),
									typeof (uint),
									typeof (RoadBaseAI.TrafficLightState),
									typeof (RoadBaseAI.TrafficLightState),
									typeof (bool),
									typeof (bool)
							},
							null),
							typeof(CustomRoadAI).GetMethod("CustomSetTrafficLightState")));
				} catch (Exception) {
					Log.Error("Could not redirect RoadBaseAI::SetTrafficLightState");
					detourFailed = true;
				}

				Log.Info("Redirection RoadBaseAI::UpdateLanes calls");
				try {
					Detours.Add(new Detour(typeof(RoadBaseAI).GetMethod("UpdateLanes",
							BindingFlags.Public | BindingFlags.Instance,
							null,
							new[]
							{
									typeof (ushort),
									typeof (NetSegment).MakeByRefType(),
									typeof (bool)
							},
							null),
							typeof(CustomRoadAI).GetMethod("CustomUpdateLanes")));
				} catch (Exception) {
					Log.Error("Could not redirect RoadBaseAI::UpdateLanes");
					detourFailed = true;
				}
				
				Log.Info("Redirection TrainAI::CheckNextLane calls");
				try {
					Detours.Add(new Detour(typeof(TrainAI).GetMethod("CheckNextLane",
							BindingFlags.NonPublic | BindingFlags.Instance,
							null,
							new[]
							{
									typeof (ushort),
									typeof (Vehicle).MakeByRefType(),
									typeof (float).MakeByRefType(),
									typeof (PathUnit.Position),
									typeof (uint),
									typeof (byte),
									typeof (PathUnit.Position),
									typeof (uint),
									typeof (byte),
									typeof (Bezier3)
							},
							null),
							typeof(CustomTrainAI).GetMethod("CustomCheckNextLane")));
				} catch (Exception) {
					Log.Error("Could not redirect TrainAI::CheckNextLane");
					detourFailed = true;
				}

				/*Log.Info("Redirection NetManager::FinalizeNode calls");
				try {
					Detours.Add(new Detour(typeof(NetManager).GetMethod("FinalizeNode",
							BindingFlags.NonPublic | BindingFlags.Instance,
							null,
							new[]
							{
									typeof (ushort),
									typeof (NetNode).MakeByRefType()
							},
							null),
							typeof(CustomNetManager).GetMethod("CustomFinalizeNode")));
				} catch (Exception) {
					Log.Error("Could not redirect NetManager::FinalizeNode");
					detourFailed = true;
				}*/

				Log.Info("Redirection NetManager::FinalizeSegment calls");
				try {
					Detours.Add(new Detour(typeof(NetManager).GetMethod("FinalizeSegment",
							BindingFlags.NonPublic | BindingFlags.Instance,
							null,
							new[]
							{
									typeof (ushort),
									typeof (NetSegment).MakeByRefType()
							},
							null),
							typeof(CustomNetManager).GetMethod("CustomFinalizeSegment")));
				} catch (Exception) {
					Log.Error("Could not redirect NetManager::FinalizeSegment");
					detourFailed = true;
				}

				Log.Info("Redirection NetManager::UpdateSegment calls");
				try {
					Detours.Add(new Detour(typeof(NetManager).GetMethod("UpdateSegment",
							BindingFlags.NonPublic | BindingFlags.Instance,
							null,
							new[]
							{
									typeof (ushort),
									typeof (ushort),
									typeof (int),
							},
							null),
							typeof(CustomNetManager).GetMethod("CustomUpdateSegment")));
				} catch (Exception) {
					Log.Error("Could not redirect NetManager::UpdateSegment");
					detourFailed = true;
				}

				if (detourFailed) {
					Log.Info("Detours failed");
					UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel").SetMessage("Incompatibility Issue", "Traffic Manager: President Edition detected an incompatibility with another mod! You can continue playing but it's NOT recommended. Traffic Manager will not work as expected.", true);
				} else {
					Log.Info("Detours successful");
				}

				DetourInited = true;
			}
		}

		internal static bool IsGameLoaded() {
			return gameLoaded;
		}

		public override void OnCreated(ILoading loading) {
            //SelfDestruct.DestructOldInstances(this);

            base.OnCreated(loading);

            ToolMode = TrafficManagerMode.None;
			Detours = new List<Detour>();
            DetourInited = false;
            CustomPathManager = new CustomPathManager();
        }

        public override void OnReleased() {
            base.OnReleased();

            if (ToolMode != TrafficManagerMode.None) {
                ToolMode = TrafficManagerMode.None;
                DestroyTool();
            }
        }

		public override void OnLevelUnloading() {
			Log.Info("OnLevelUnloading");
			base.OnLevelUnloading();
			Instance = this;
			if (IsPathManagerReplaced) {
				Singleton<PathManager>.instance.WaitForAllPaths();
			}
			revertDetours();
			gameLoaded = false;

			Object.Destroy(UI);
			UI = null;

			try {
				TrafficPriorityManager.Instance().OnLevelUnloading();
				CustomCarAI.OnLevelUnloading();
				CustomRoadAI.OnLevelUnloading();
				CustomTrafficLightsManager.Instance().OnLevelUnloading();
				TrafficLightSimulationManager.Instance().OnLevelUnloading();
				VehicleRestrictionsManager.Instance().OnLevelUnloading();
				Flags.OnLevelUnloading();
				Translation.OnLevelUnloading();
#if TRACE
				Singleton<CodeProfiler>.instance.OnLevelUnloading();
#endif
			} catch (Exception e) {
				Log.Error("Exception unloading mod. " + e.Message);
				// ignored - prevents collision with other mods
			}
		}

		public override void OnLevelLoaded(LoadMode mode) {
            Log.Info("OnLevelLoaded");
            base.OnLevelLoaded(mode);

            Log._Debug("OnLevelLoaded Returned from base, calling custom code.");
			Instance = this;

			gameLoaded = false;
            switch (mode) {
                case LoadMode.NewGame:
                case LoadMode.LoadGame:
					if (BuildConfig.applicationVersion != BuildConfig.VersionToString(TrafficManagerMod.GameVersion, false)) {
						string[] majorVersionElms = BuildConfig.applicationVersion.Split('-');
						string[] versionElms = majorVersionElms[0].Split('.');
						uint versionA = Convert.ToUInt32(versionElms[0]);
						uint versionB = Convert.ToUInt32(versionElms[1]);
						uint versionC = Convert.ToUInt32(versionElms[2]);

						bool isModTooOld = TrafficManagerMod.GameVersionA < versionA ||
							(TrafficManagerMod.GameVersionA == versionA && TrafficManagerMod.GameVersionB < versionB) ||
							(TrafficManagerMod.GameVersionA == versionA && TrafficManagerMod.GameVersionB == versionB && TrafficManagerMod.GameVersionC < versionC);

						if (isModTooOld) {
							UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel").SetMessage("TM:PE has not been updated yet", $"Traffic Manager: President Edition detected that you are running a newer game version ({BuildConfig.applicationVersion}) than TM:PE has been built for ({BuildConfig.VersionToString(TrafficManagerMod.GameVersion, false)}). Please be aware that TM:PE has not been updated for the newest game version yet and thus it is very likely it will not work as expected.", false);
						} else {
							UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel").SetMessage("Your game should be updated", $"Traffic Manager: President Edition has been built for game version {BuildConfig.VersionToString(TrafficManagerMod.GameVersion, false)}. You are running game version {BuildConfig.applicationVersion}. Some features of TM:PE will not work with older game versions. Please let Steam update your game.", false);
						}
					}
					gameLoaded = true;
					break;
				default:
					return;
            }

#if !TAM
			determinePathManagerCompatible();
			IsRainfallLoaded = CheckRainfallIsLoaded();
#if DEBUG
			SpeedLimitManager.Instance().GetDefaultSpeedLimits();
#endif

			if (IsPathManagerCompatible && ! IsPathManagerReplaced) {
				try {
					Log.Info("Pathfinder Compatible. Setting up CustomPathManager and SimManager.");
					var pathManagerInstance = typeof(Singleton<PathManager>).GetField("sInstance", BindingFlags.Static | BindingFlags.NonPublic);

					var stockPathManager = PathManager.instance;
					Log._Debug($"Got stock PathManager instance {stockPathManager.GetName()}");

					CustomPathManager = stockPathManager.gameObject.AddComponent<CustomPathManager>();
					Log._Debug("Added CustomPathManager to gameObject List");

					if (CustomPathManager == null) {
						Log.Error("CustomPathManager null. Error creating it.");
						return;
					}

					CustomPathManager.UpdateWithPathManagerValues(stockPathManager);
					Log._Debug("UpdateWithPathManagerValues success");

					pathManagerInstance?.SetValue(null, CustomPathManager);

					Log._Debug("Getting Current SimulationManager");
					var simManager =
						typeof(SimulationManager).GetField("m_managers", BindingFlags.Static | BindingFlags.NonPublic)?
							.GetValue(null) as FastList<ISimulationManager>;

					Log._Debug("Removing Stock PathManager");
					simManager?.Remove(stockPathManager);

					Log._Debug("Adding Custom PathManager");
					simManager?.Add(CustomPathManager);

					Object.Destroy(stockPathManager, 10f);

					Log._Debug("Should be custom: " + Singleton<PathManager>.instance.GetType().ToString());

					IsPathManagerReplaced = true;
				} catch (Exception ex) {
					Log.Error($"Path manager replacement error: {ex.ToString()}");
					UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel").SetMessage("Incompatibility Issue", "Traffic Manager: President Edition detected an incompatibility with another mod! You can continue playing but it's NOT recommended. Traffic Manager will not work as expected.", true);
					IsPathManagerCompatible = false;
				}
			}

			Log.Info("Adding Controls to UI.");
			UI = ToolsModifierControl.toolController.gameObject.AddComponent<UIBase>();

			initDetours();
			Log.Info("OnLevelLoaded complete.");
#endif
		}


#if !TAM
		private void determinePathManagerCompatible() {
			IsPathManagerCompatible = true;
			if (!IsPathManagerReplaced) {

				var loadingWrapperLoadingExtensionsField = typeof(LoadingWrapper).GetField("m_LoadingExtensions", BindingFlags.NonPublic | BindingFlags.Instance);
				List<ILoadingExtension> loadingExtensions = null;
				if (loadingWrapperLoadingExtensionsField != null) {
					loadingExtensions = (List<ILoadingExtension>) loadingWrapperLoadingExtensionsField.GetValue(Singleton<LoadingManager>.instance.m_LoadingWrapper);
				} else {
					Log._Debug("Could not get loading extensions field");
				}

				if (loadingExtensions != null) {
					Log.Info("Loaded extensions:");
					foreach (ILoadingExtension extension in loadingExtensions) {
						if (extension.GetType().Namespace == null)
							continue;

						Log.Info($"type: {extension.GetType().ToString()} type namespace: {extension.GetType().Namespace.ToString()} toString: {extension.ToString()}");
						var namespaceStr = extension.GetType().Namespace.ToString();
						if ("Improved_AI".Equals(namespaceStr) || "CSL_Traffic".Equals(namespaceStr)) {
							IsPathManagerCompatible = false; // Improved AI found
							Log.Info($"type: {extension.GetType().ToString()} type namespace: {extension.GetType().Namespace.ToString()} toString: {extension.ToString()}. Custom PathManager detected.");
						}
					}
				} else {
					Log._Debug("Could not get loading extensions");
				}

				if (Singleton<PathManager>.instance.GetType() != typeof(PathManager)) {
					Log.Info("PathManager manipulation detected. Disabling custom PathManager " + Singleton<PathManager>.instance.GetType().ToString());
					IsPathManagerCompatible = false;
				}
			}

			if (!IsPathManagerCompatible) {
				Options.setAdvancedAI(false);
			}
		}
#endif

		private bool CheckRainfallIsLoaded() {
			bool rainfall = false;

			var loadingWrapperLoadingExtensionsField = typeof(LoadingWrapper).GetField("m_LoadingExtensions", BindingFlags.NonPublic | BindingFlags.Instance);
			List<ILoadingExtension> loadingExtensions = null;
			if (loadingWrapperLoadingExtensionsField != null) {
				loadingExtensions = (List<ILoadingExtension>)loadingWrapperLoadingExtensionsField.GetValue(Singleton<LoadingManager>.instance.m_LoadingWrapper);
			} else {
				Log._Debug("Could not get loading extensions field");
			}

			if (loadingExtensions != null) {
				foreach (ILoadingExtension extension in loadingExtensions) {
					if (extension.GetType().Namespace == null)
						continue;

					var namespaceStr = extension.GetType().Namespace.ToString();
					if ("Rainfall".Equals(namespaceStr)) {
						Log.Info("The mod Rainfall has been detected.");
						rainfall = true;
						break;
					}
				}
			} else {
				Log._Debug("Could not get loading extensions");
			}

			return rainfall;
		}

		public void SetToolMode(TrafficManagerMode mode) {
            if (mode == ToolMode) return;

            ToolMode = mode;

            if (mode != TrafficManagerMode.None) {
                EnableTool();
            } else {
				DisableTool();
            }
        }

        public void EnableTool() {
            if (TrafficManagerTool == null) {
                TrafficManagerTool = ToolsModifierControl.toolController.gameObject.GetComponent<TrafficManagerTool>() ??
                                   ToolsModifierControl.toolController.gameObject.AddComponent<TrafficManagerTool>();
            }

            ToolsModifierControl.toolController.CurrentTool = TrafficManagerTool;
            ToolsModifierControl.SetTool<TrafficManagerTool>();
        }

		public void DisableTool() {
			ToolsModifierControl.toolController.CurrentTool = ToolsModifierControl.GetTool<DefaultTool>();
			ToolsModifierControl.SetTool<DefaultTool>();
		}

		private void DestroyTool() {
			if (ToolsModifierControl.toolController != null) {
				ToolsModifierControl.toolController.CurrentTool = ToolsModifierControl.GetTool<DefaultTool>();
				ToolsModifierControl.SetTool<DefaultTool>();

				if (TrafficManagerTool != null) {
					Object.Destroy(TrafficManagerTool);
					TrafficManagerTool = null;
				}
			} else
				Log.Warning("LoadingExtensions.DestroyTool: ToolsModifierControl.toolController is null!");
        }
	}
}
