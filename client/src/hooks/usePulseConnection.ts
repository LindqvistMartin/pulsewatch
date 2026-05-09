import { useEffect, useState } from 'react'
import { HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr'
import { useQueryClient } from '@tanstack/react-query'

interface PulseConnectionResult {
  connectionState: HubConnectionState
}

export function usePulseConnection(projectId: string | null): PulseConnectionResult {
  const [connectionState, setConnectionState] = useState<HubConnectionState>(
    HubConnectionState.Disconnected,
  )
  const queryClient = useQueryClient()

  useEffect(() => {
    if (!projectId) {
      setConnectionState(HubConnectionState.Disconnected)
      return
    }

    let stopped = false

    const connection = new HubConnectionBuilder()
      .withUrl((import.meta.env.VITE_API_URL as string) + '/hubs/pulse')
      .withAutomaticReconnect()
      .build()

    connection.onreconnecting(() => {
      if (!stopped) setConnectionState(HubConnectionState.Reconnecting)
    })
    connection.onreconnected(() => {
      if (!stopped) setConnectionState(HubConnectionState.Connected)
    })
    connection.onclose(() => {
      if (!stopped) setConnectionState(HubConnectionState.Disconnected)
    })

    connection.on('HealthCheckRecorded', (payload: { probeId: string }) => {
      void queryClient.invalidateQueries({ queryKey: ['probes', projectId] })
      void queryClient.invalidateQueries({ queryKey: ['checks', payload.probeId] })
    })

    setConnectionState(HubConnectionState.Connecting)

    connection
      .start()
      .then(() => {
        if (!stopped) {
          setConnectionState(HubConnectionState.Connected)
          return connection.invoke('JoinProject', projectId)
        }
      })
      .catch(() => {
        if (!stopped) setConnectionState(HubConnectionState.Disconnected)
      })

    return () => {
      stopped = true
      connection.off('HealthCheckRecorded')
      if (connection.state === HubConnectionState.Connected) {
        void connection.invoke('LeaveProject', projectId).catch(() => {})
      }
      void connection.stop()
    }
  }, [projectId, queryClient])

  return { connectionState }
}
