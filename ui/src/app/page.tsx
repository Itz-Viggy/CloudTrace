"use client"

import React, { useEffect, useState } from 'react'
import {
  Activity,
  AlertTriangle,
  CheckCircle,
  Clock,
  ShieldAlert,
  Zap,
  BarChart3,
  Terminal,
  ChevronRight,
  RefreshCw,
  Cpu,
  Database
} from 'lucide-react'
import { formatDistanceToNow } from 'date-fns'
import { motion, AnimatePresence } from 'framer-motion'
import { clsx, type ClassValue } from 'clsx'
import { twMerge } from 'tailwind-merge'
import { Incident, MetricOverview, API_BASE_URL } from './types'

function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

export default function Dashboard() {
  const [metrics, setMetrics] = useState<MetricOverview | null>(null)
  const [incidents, setIncidents] = useState<Incident[]>([])
  const [selectedIncident, setSelectedIncident] = useState<Incident | null>(null)
  const [loading, setLoading] = useState(true)
  const [refreshing, setRefreshing] = useState(false)

  const fetchData = async () => {
    try {
      setRefreshing(true)
      const [mRes, iRes] = await Promise.all([
        fetch(`${API_BASE_URL}/metrics/overview`),
        fetch(`${API_BASE_URL}/incidents`)
      ])

      if (mRes.ok) setMetrics(await mRes.json())
      if (iRes.ok) setIncidents(await iRes.json())
    } catch (err) {
      console.error("Fetch error:", err)
    } finally {
      setLoading(false)
      setRefreshing(false)
    }
  }

  useEffect(() => {
    fetchData()
    const interval = setInterval(fetchData, 30000) // Auto refresh every 30s
    return () => clearInterval(interval)
  }, [])

  if (loading) {
    return (
      <div className="flex h-screen w-full items-center justify-center bg-[#0a0a0b] text-white">
        <div className="flex flex-col items-center gap-4">
          <RefreshCw className="h-8 w-8 animate-spin text-lime-400" />
          <p className="text-zinc-500 animate-pulse font-medium">Initializing CloudTrace Core...</p>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-[#0a0a0b] text-zinc-100 p-6 font-sans selection:bg-lime-500 selection:text-black">
      {/* Header */}
      <header className="max-w-7xl mx-auto flex items-center justify-between mb-8">
        <div className="flex items-center gap-3">
          <div className="bg-lime-500/10 p-2 rounded-lg border border-lime-500/20">
            <ShieldAlert className="h-6 w-6 text-lime-400" />
          </div>
          <div>
            <h1 className="text-xl font-bold tracking-tight">CloudTrace <span className="text-zinc-500 font-normal">v1.0</span></h1>
            <p className="text-xs text-zinc-500 uppercase tracking-widest font-semibold mt-0.5">Real-time Anomaly Intelligence</p>
          </div>
        </div>
        <div className="flex items-center gap-4">
          <button
            onClick={fetchData}
            className={cn(
              "p-2 rounded-full hover:bg-zinc-800 transition-colors",
              refreshing && "animate-spin text-lime-400"
            )}
          >
            <RefreshCw className="h-5 w-5" />
          </button>
          <div className="flex items-center gap-2 px-3 py-1.5 bg-zinc-900 border border-zinc-800 rounded-full">
            <div className="h-2 w-2 rounded-full bg-emerald-500 status-pulse" />
            <span className="text-[10px] uppercase font-bold tracking-wider text-zinc-400">System Live</span>
          </div>
        </div>
      </header>

      <main className="max-w-7xl mx-auto grid grid-cols-1 lg:grid-cols-12 gap-6">

        {/* Left Column: Metrics and List */}
        <div className="lg:col-span-4 space-y-6">
          {/* Quick Metrics */}
          <section className="grid grid-cols-2 gap-4">
            <MetricCard
              label="Error Rate"
              value={`${((metrics?.error_rate || 0) * 100).toFixed(1)}%`}
              icon={<AlertTriangle className="h-4 w-4" />}
              color={metrics?.error_rate && metrics.error_rate > 0.1 ? "text-red-400" : "text-lime-400"}
            />
            <MetricCard
              label="Avg Latency"
              value={`${Math.round(metrics?.avg_latency || 0)}ms`}
              icon={<Zap className="h-4 w-4" />}
              color="text-amber-400"
            />
          </section>

          {/* Incident Feed */}
          <section className="bg-zinc-900/50 border border-zinc-800 rounded-xl overflow-hidden flex flex-col min-h-[500px]">
            <div className="p-4 border-b border-zinc-800 flex justify-between items-center bg-zinc-900/80 backdrop-blur-md sticky top-0 z-10">
              <h2 className="text-sm font-bold uppercase tracking-wider text-zinc-400">Active Incidents</h2>
              <span className="text-[10px] bg-zinc-800 px-2 py-0.5 rounded text-zinc-500 font-mono">
                {incidents.length} EVENTS
              </span>
            </div>
            <div className="flex-grow overflow-y-auto max-h-[600px]">
              {incidents.length === 0 ? (
                <div className="flex flex-col items-center justify-center p-12 text-zinc-600 grayscale opacity-50">
                  <CheckCircle className="h-10 w-10 mb-2" />
                  <p className="text-sm">No active threats detected</p>
                </div>
              ) : (
                incidents.map((incident) => (
                  <button
                    key={incident.id}
                    onClick={() => setSelectedIncident(incident)}
                    className={cn(
                      "w-full p-4 border-b border-zinc-800/50 flex items-start gap-4 text-left transition-all hover:bg-zinc-800/50",
                      selectedIncident?.id === incident.id && "bg-lime-500/5 border-l-2 border-l-lime-500"
                    )}
                  >
                    <div className={cn(
                      "p-1.5 rounded bg-zinc-800 shrink-0",
                      incident.severity === 'CRITICAL' ? 'text-red-400 bg-red-400/10' :
                        incident.severity === 'WARNING' ? 'text-amber-400 bg-amber-400/10' : 'text-blue-400'
                    )}>
                      <Activity className="h-4 w-4" />
                    </div>
                    <div className="flex-grow min-w-0">
                      <div className="flex justify-between items-start mb-0.5">
                        <span className="text-xs font-mono text-zinc-500 truncate">{incident.id}</span>
                        <span className="text-[10px] font-medium text-zinc-600 uppercase">
                          {formatDistanceToNow(new Date(typeof incident.start_ts === 'string' ? incident.start_ts : ((incident.start_ts as any).seconds * 1000)), { addSuffix: true })}
                        </span>
                      </div>
                      <h3 className="text-sm font-bold truncate text-zinc-200">{incident.service}</h3>
                      <p className="text-xs text-zinc-500 mt-1 uppercase tracking-tighter font-semibold flex items-center gap-1.5">
                        <span className={cn(
                          "h-1 w-1 rounded-full",
                          incident.severity === 'CRITICAL' ? 'bg-red-400' : 'bg-amber-400'
                        )} />
                        {incident.anomaly_type.replace('_', ' ')}
                      </p>
                    </div>
                  </button>
                ))
              )}
            </div>
          </section>
        </div>

        {/* Right Column: Detail View */}
        <div className="lg:col-span-8">
          <AnimatePresence mode="wait">
            {selectedIncident ? (
              <motion.div
                key={selectedIncident.id}
                initial={{ opacity: 0, y: 10 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0, y: -10 }}
                className="space-y-6"
              >
                {/* Detail Card */}
                <section className="glass-card overflow-hidden">
                  <div className="p-6 border-b border-zinc-800 bg-zinc-900/30">
                    <div className="flex items-center gap-2 mb-4">
                      <span className={cn(
                        "px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-widest",
                        selectedIncident.severity === 'CRITICAL' ? 'bg-red-500/10 text-red-500 border border-red-500/20' :
                          'bg-amber-500/10 text-amber-500 border border-amber-500/20'
                      )}>
                        {selectedIncident.severity}
                      </span>
                      <span className="text-zinc-600 text-[10px] font-mono">{selectedIncident.id}</span>
                    </div>
                    <h2 className="text-3xl font-extrabold text-white mb-2">{selectedIncident.service}</h2>
                    <p className="text-zinc-400 flex items-center gap-2 text-sm">
                      <Clock className="h-4 w-4" />
                      Detected at {new Date(typeof selectedIncident.start_ts === 'string' ? selectedIncident.start_ts : ((selectedIncident.start_ts as any).seconds * 1000)).toLocaleString()}
                    </p>
                  </div>

                  {/* AI Analysis Section */}
                  <div className="p-8 space-y-8">
                    {selectedIncident.ai_status === 'COMPLETED' ? (
                      <>
                        <div>
                          <h3 className="text-xs font-bold text-lime-400 uppercase tracking-widest mb-3 flex items-center gap-2">
                            Gemini Analysis Engine
                          </h3>
                          <div className="prose prose-invert max-w-none">
                            <p className="text-zinc-200 leading-relaxed text-lg font-medium italic">
                              "{selectedIncident.ai_summary}"
                            </p>
                          </div>
                        </div>

                        <div className="grid grid-cols-1 md:grid-cols-2 gap-8 pt-6 border-t border-zinc-800">
                          <div>
                            <h4 className="text-[10px] font-bold text-zinc-500 uppercase tracking-widest mb-4">Root Cause Suspect</h4>
                            <div className="p-4 bg-red-950/20 border border-red-500/20 rounded-lg">
                              <p className="text-sm text-red-100/80 leading-relaxed">
                                {selectedIncident.ai_root_cause}
                              </p>
                            </div>
                          </div>
                          <div>
                            <h4 className="text-[10px] font-bold text-zinc-500 uppercase tracking-widest mb-4">Recommended Mitigation</h4>
                            <ul className="space-y-3">
                              {selectedIncident.ai_steps?.map((step, i) => (
                                <li key={i} className="flex items-start gap-3 text-sm text-zinc-300">
                                  <div className="mt-1 h-4 w-4 rounded-full bg-lime-500/10 border border-lime-500/30 flex items-center justify-center text-[10px] font-bold text-lime-400 shrink-0">
                                    {i + 1}
                                  </div>
                                  {step}
                                </li>
                              ))}
                            </ul>
                          </div>
                        </div>

                        {selectedIncident.debugging_queries && selectedIncident.debugging_queries.length > 0 && (
                          <div className="pt-6 border-t border-zinc-800">
                            <h4 className="text-[10px] font-bold text-zinc-500 uppercase tracking-widest mb-4 flex items-center gap-2">
                              <Terminal className="h-3 w-3" /> Incident Forensics
                            </h4>
                            <div className="bg-black/40 p-4 rounded-lg font-mono text-[11px] text-lime-500/80 overflow-x-auto border border-zinc-800/50">
                              {selectedIncident.debugging_queries[0]}
                            </div>
                          </div>
                        )}
                      </>
                    ) : (
                      <div className="flex flex-col items-center justify-center py-20 text-center">
                        <div className="relative mb-6">
                          <div className="h-16 w-16 border-4 border-lime-500/20 rounded-full animate-spin border-t-lime-500" />
                          <Cpu className="h-6 w-6 text-lime-400 absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2" />
                        </div>
                        <h3 className="text-lg font-bold text-white mb-2">Analyzing Evidence</h3>
                        <p className="text-zinc-500 text-sm max-w-xs">Gemini 2.5 is correlating log patterns and calculating blast radius...</p>
                      </div>
                    )}
                  </div>
                </section>

                {/* Evidence Stats Cards */}
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                  <StatItem label="Error Spike" value={`${selectedIncident.error_count} events`} icon={<AlertTriangle className="h-4 w-4" />} color="text-red-400" />
                  <StatItem label="Baseline Rate" value={`${(selectedIncident.baseline_rate * 100).toFixed(1)}%`} icon={<Database className="h-4 w-4" />} />
                  <StatItem label="Current Rate" value={`${(selectedIncident.current_rate * 100).toFixed(1)}%`} icon={<BarChart3 className="h-4 w-4" />} color="text-red-400" />
                </div>
              </motion.div>
            ) : (
              <div className="h-full min-h-[600px] flex flex-col items-center justify-center text-center opacity-40 border-2 border-dashed border-zinc-800 rounded-xl">
                <ShieldAlert className="h-20 w-20 mb-4 text-zinc-700" />
                <h2 className="text-2xl font-bold text-zinc-500">System Monitoring Active</h2>
                <p className="text-zinc-600 mt-2 max-w-xs">Select an incident from the feed to view real-time AI forensics</p>
              </div>
            )}
          </AnimatePresence>
        </div>
      </main>
    </div>
  )
}

function MetricCard({ label, value, icon, color }: { label: string, value: string, icon: React.ReactNode, color?: string }) {
  return (
    <div className="bg-zinc-900 border border-zinc-800 p-4 rounded-xl">
      <div className="flex items-center gap-2 text-zinc-500 mb-2 uppercase text-[10px] font-bold tracking-widest">
        {icon} {label}
      </div>
      <div className={cn("text-2xl font-black tabular-nums tracking-tighter", color || "text-white")}>
        {value}
      </div>
    </div>
  )
}

function StatItem({ label, value, icon, color }: { label: string, value: string, icon: React.ReactNode, color?: string }) {
  return (
    <div className="bg-zinc-900/40 border border-zinc-800/60 p-4 rounded-xl">
      <div className="flex items-center gap-2 text-zinc-500 mb-1 uppercase text-[9px] font-bold tracking-widest">
        {icon} {label}
      </div>
      <div className={cn("text-lg font-bold tabular-nums", color || "text-zinc-200")}>
        {value}
      </div>
    </div>
  )
}
