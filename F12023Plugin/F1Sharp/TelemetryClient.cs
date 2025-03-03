﻿using F1Sharp.Packets;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Timer = System.Timers.Timer;

namespace F1Sharp
{
    public class TelemetryClient
    {
        /// <summary>
        /// Time required to timeout in milliseconds
        /// </summary>
        private const float TIMEOUT = 500.0f;

        private UdpClient _client;
        private IPEndPoint _peerEndPoint;
        private Timer _timeoutTimer;
        private GCHandle _handle;
        private Packet[] _defaultPackets = new Packet[] {
            Packet.CAR_DAMAGE, Packet.CAR_STATUS, Packet.CAR_TELEMETRY,
            Packet.LAP_DATA, Packet.SESSION, Packet.SESSION_HISTORY,
            Packet.EVENT, Packet.FINAL_CLASSIFICATION, Packet.PARTICIPANTS,
            Packet.MOTION, Packet.MOTION_EX, Packet.TYRE_SET,
            Packet.CAR_SETUPS, Packet.LOBBY_INFO
        };

        /// <summary>
        /// Indicates if we're currently connected
        /// </summary>
        public bool Connected { get; private set; }

        /// <summary>
        /// Packets enabled for parsing
        /// </summary>
        public Packet[] EnabledPackets { get; private set; }

        /// <summary>
        /// Connection status change delegate
        /// </summary>
        /// <param name="connected">True if connected, False if disconnected</param>
        public delegate void ConnectedStatusChangeDelegate(bool connected);

        /// <summary>
        /// Called when connect status changes
        /// </summary>
        public event ConnectedStatusChangeDelegate OnConnectedStatusChange;

        // Delegates
        public delegate void MotionDataReceiveDelegate(MotionPacket packet);
        public delegate void LapDataReceiveDelegate(LapDataPacket packet);
        public delegate void EventDetailsReceiveDelegate(EventPacket packet);
        public delegate void SessionDataReceiveDelegate(SessionPacket packet);
        public delegate void ParticipantsDataReceiveDelegate(ParticipantsPacket packet);
        public delegate void CarSetupDataReceiveDelegate(CarSetupPacket packet);
        public delegate void CarTelemetryDataReceiveDelegate(CarTelemetryPacket packet);
        public delegate void CarStatusDataReceiveDelegate(CarStatusPacket packet);
        public delegate void FinalClassificationDataReceiveDelegate(FinalClassificationPacket packet);
        public delegate void LobbyInfoDataReceiveDelegate(LobbyInfoPacket packet);
        public delegate void CarDamageDataReceiveDelegate(CarDamagePacket packet);
        public delegate void SessionHistoryDataReceiveDelegate(SessionHistoryPacket packet);
        public delegate void TyreSetDataReceiveDelegate(TyreSetPacket packet);
        public delegate void MotionExDataReceiveDelegate(MotionExPacket packet);

        // Events
        public event MotionDataReceiveDelegate OnMotionDataReceive;
        public event LapDataReceiveDelegate OnLapDataReceive;
        public event EventDetailsReceiveDelegate OnEventDetailsReceive;
        public event SessionDataReceiveDelegate OnSessionDataReceive;
        public event ParticipantsDataReceiveDelegate OnParticipantsDataReceive;
        public event CarSetupDataReceiveDelegate OnCarSetupDataReceive;
        public event CarTelemetryDataReceiveDelegate OnCarTelemetryDataReceive;
        public event CarStatusDataReceiveDelegate OnCarStatusDataReceive;
        public event FinalClassificationDataReceiveDelegate OnFinalClassificationDataReceive;
        public event LobbyInfoDataReceiveDelegate OnLobbyInfoDataReceive;
        public event CarDamageDataReceiveDelegate OnCarDamageDataReceive;
        public event SessionHistoryDataReceiveDelegate OnSessionHistoryDataReceive;
        public event TyreSetDataReceiveDelegate OnTyreSetDataReceive;
        public event MotionExDataReceiveDelegate OnMotionExDataReceive;

        /// <summary>
        /// Constructs client and sets it up for receiving data
        /// </summary>
        /// <param name="port">The port to listen to. This must match your game setting.</param>
        public TelemetryClient(int port, Packet[] enabledPackets = null)
        {
            _client = new UdpClient(port);
            _peerEndPoint = new IPEndPoint(IPAddress.Any, port);

            _timeoutTimer = new Timer(TIMEOUT)
            {
                AutoReset = true
            };
            _timeoutTimer.Elapsed += TimeoutEvent;

            Connected = true;
            OnConnectedStatusChange?.Invoke(true);

            EnabledPackets = enabledPackets ?? _defaultPackets;

            _client.BeginReceive(new AsyncCallback(ReceiveCallback), null);
        }

        public void Stop()
        {
            Connected = false;
            OnConnectedStatusChange?.Invoke(false);

            _client.Close();
            _client = null;

            if (_handle.IsAllocated)
            {
                _handle.Free();
            }
        }

        /// <summary>
        /// Called when no data is received in a period of time
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Elapsed event arguments</param>
        private void TimeoutEvent(object sender, System.Timers.ElapsedEventArgs e)
        {
            Connected = false;
            OnConnectedStatusChange?.Invoke(false);
        }

        /// <summary>
        /// Called when data is received
        /// </summary>
        /// <param name="result">Resulting data</param>
        /// <exception cref="Exception">Something went wrong</exception>
        private void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                    byte[] data = _client.EndReceive(result, ref _peerEndPoint);

                    _handle = GCHandle.Alloc(data, GCHandleType.Pinned);

                    PacketHeader header = (PacketHeader)Marshal.PtrToStructure(_handle.AddrOfPinnedObject(), typeof(PacketHeader));

                    if (EnabledPackets.Contains(header.packetId))
                    {
                        switch (header.packetId)
                        {
                            case Packet.MOTION:
                                MotionPacket motionPacket = (MotionPacket)Marshal.PtrToStructure(_handle.AddrOfPinnedObject(), typeof(MotionPacket));
                                OnMotionDataReceive?.Invoke(motionPacket);
                                break;
                            case Packet.LAP_DATA:
                                LapDataPacket lapDataPacket = (LapDataPacket)Marshal.PtrToStructure(_handle.AddrOfPinnedObject(), typeof(LapDataPacket));
                                OnLapDataReceive?.Invoke(lapDataPacket);
                                break;
                            case Packet.EVENT:
                                EventPacket eventPacket = (EventPacket)Marshal.PtrToStructure(_handle.AddrOfPinnedObject(), typeof(EventPacket));
                                OnEventDetailsReceive?.Invoke(eventPacket);
                                break;
                            case Packet.SESSION:
                                SessionPacket sessionPacket = (SessionPacket)Marshal.PtrToStructure(_handle.AddrOfPinnedObject(), typeof(SessionPacket));
                                OnSessionDataReceive?.Invoke(sessionPacket);
                                break;
                            case Packet.PARTICIPANTS:
                                ParticipantsPacket participantsPacket = (ParticipantsPacket)Marshal.PtrToStructure(_handle.AddrOfPinnedObject(), typeof(ParticipantsPacket));
                                OnParticipantsDataReceive?.Invoke(participantsPacket);
                                break;
                            case Packet.CAR_SETUPS:
                                CarSetupPacket carSetupPacket = (CarSetupPacket)Marshal.PtrToStructure(_handle.AddrOfPinnedObject(), typeof(CarSetupPacket));
                                OnCarSetupDataReceive?.Invoke(carSetupPacket);
                                break;
                            case Packet.CAR_TELEMETRY:
                                CarTelemetryPacket carTelemetryPacket = (CarTelemetryPacket)Marshal.PtrToStructure(_handle.AddrOfPinnedObject(), typeof(CarTelemetryPacket));
                                OnCarTelemetryDataReceive?.Invoke(carTelemetryPacket);
                                break;
                            case Packet.CAR_STATUS:
                                CarStatusPacket carStatusPacket = (CarStatusPacket)Marshal.PtrToStructure(_handle.AddrOfPinnedObject(), typeof(CarStatusPacket));
                                OnCarStatusDataReceive?.Invoke(carStatusPacket);
                                break;
                            case Packet.FINAL_CLASSIFICATION:
                                FinalClassificationPacket finalClassificationPacket = (FinalClassificationPacket)Marshal.PtrToStructure(_handle.AddrOfPinnedObject(), typeof(FinalClassificationPacket));
                                OnFinalClassificationDataReceive?.Invoke(finalClassificationPacket);
                                break;
                            case Packet.LOBBY_INFO:
                                LobbyInfoPacket lobbyInfoPacket = (LobbyInfoPacket)Marshal.PtrToStructure(_handle.AddrOfPinnedObject(), typeof(LobbyInfoPacket));
                                OnLobbyInfoDataReceive?.Invoke(lobbyInfoPacket);
                                break;
                            case Packet.CAR_DAMAGE:
                                CarDamagePacket carDamagePacket = (CarDamagePacket)Marshal.PtrToStructure(_handle.AddrOfPinnedObject(), typeof(CarDamagePacket));
                                OnCarDamageDataReceive?.Invoke(carDamagePacket);
                                break;
                            case Packet.SESSION_HISTORY:
                                SessionHistoryPacket sessionHistoryPacket = (SessionHistoryPacket)Marshal.PtrToStructure(_handle.AddrOfPinnedObject(), typeof(SessionHistoryPacket));
                                OnSessionHistoryDataReceive?.Invoke(sessionHistoryPacket);
                                break;
                            case Packet.TYRE_SET:
                                TyreSetPacket tyreSetPacket = (TyreSetPacket)Marshal.PtrToStructure(_handle.AddrOfPinnedObject(), typeof(TyreSetPacket));
                                OnTyreSetDataReceive?.Invoke(tyreSetPacket);
                                break;
                            case Packet.MOTION_EX:
                                MotionExPacket motionExPacket = (MotionExPacket)Marshal.PtrToStructure(_handle.AddrOfPinnedObject(), typeof(MotionExPacket));
                                OnMotionExDataReceive?.Invoke(motionExPacket);
                                break;
                        }
                    }
                
            }
            catch
            {
                // Ignore
            }
            finally {
                if (_handle.IsAllocated)
                {
                    _handle.Free();
                }

                _client?.BeginReceive(new AsyncCallback(ReceiveCallback), null);
            }
        }
    }
}