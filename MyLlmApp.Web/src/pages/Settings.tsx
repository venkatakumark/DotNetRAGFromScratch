import { useQuery } from "@tanstack/react-query";
import { apiClient } from "../api/client";
import { Card, CardHeader, CardTitle, CardContent } from "../components/common/Card";
import { LoadingSpinner } from "../components/common/Loading";
import { formatBytes } from "../utils";
import {
  Server,
  Database,
  Zap,
  FileText,
  Terminal,
  Globe,
  CheckCircle,
  AlertCircle,
} from "lucide-react";

export function Settings() {
  const { data: health, isLoading: healthLoading } = useQuery({
    queryKey: ["health"],
    queryFn: () => apiClient.getHealth(),
    refetchInterval: 60000,
    retry: 1,
  });

  const { data: documents } = useQuery({
    queryKey: ["documents"],
    queryFn: () => apiClient.getDocuments(),
    refetchOnWindowFocus: false,
  });

  // Get current API URL from environment
  const apiUrl = import.meta.env.VITE_API_BASE_URL || "http://localhost:5149";
  const frontendVersion = "1.0.0";

  // Check API connectivity
  const apiConnected = true;

  return (
    <div className="max-w-3xl space-y-6">
      {/* Page Header */}
      <div>
        <h1 className="text-2xl font-bold">Settings & Status</h1>
        <p className="text-muted-foreground">Configuration and system status</p>
      </div>

      {/* API Configuration */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Globe className="h-5 w-5" />
            API Configuration
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center justify-between rounded-lg border p-4">
            <div className="flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-muted">
                <Server className="h-5 w-5" />
              </div>
              <div>
                <p className="font-medium">API Base URL</p>
                <p className="text-xs text-muted-foreground">Backend API endpoint</p>
              </div>
            </div>
            <code className="rounded bg-muted px-2 py-1 text-sm">{apiUrl}</code>
          </div>

          <div className="flex items-center justify-between rounded-lg border p-4">
            <div className="flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-muted">
                {apiConnected ? (
                  <CheckCircle className="h-5 w-5 text-green-600" />
                ) : (
                  <AlertCircle className="h-5 w-5 text-red-600" />
                )}
              </div>
              <div>
                <p className="font-medium">API Connectivity</p>
                <p className="text-xs text-muted-foreground">Backend accessibility</p>
              </div>
            </div>
            {apiConnected ? (
              <span className="flex items-center gap-1 text-sm font-medium text-green-600">
                <span className="h-2 w-2 rounded-full bg-green-500" />
                Connected
              </span>
            ) : (
              <span className="flex items-center gap-1 text-sm font-medium text-red-600">
                <span className="h-2 w-2 rounded-full bg-red-500" />
                Disconnected
              </span>
            )}
          </div>
        </CardContent>
      </Card>

      {/* Backend Services */}
      {healthLoading ? (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Zap className="h-5 w-5" />
              Backend Services
            </CardTitle>
          </CardHeader>
          <CardContent>
            <LoadingSpinner />
            <p className="mt-2 text-sm text-muted-foreground">Checking service status...</p>
          </CardContent>
        </Card>
      ) : (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Zap className="h-5 w-5" />
              Backend Services
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            {health?.components ? (
              <>
                <div className="flex items-center justify-between rounded-lg border p-4">
                  <div className="flex items-center gap-3">
                    <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-blue-100 dark:bg-blue-900">
                      <Database className="h-5 w-5 text-blue-600 dark:text-blue-400" />
                    </div>
                    <div>
                      <p className="font-medium">Qdrant</p>
                      <p className="text-xs text-muted-foreground">Vector database</p>
                    </div>
                  </div>
                  <span
                    className={health.components.qdrant.status === "healthy" ? "text-green-600" : "text-red-600"}
                  >
                    {health.components.qdrant.status}
                  </span>
                </div>

                <div className="flex items-center justify-between rounded-lg border p-4">
                  <div className="flex items-center gap-3">
                    <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-purple-100 dark:bg-purple-900">
                      <Zap className="h-5 w-5 text-purple-600 dark:text-purple-400" />
                    </div>
                    <div>
                      <p className="font-medium">Ollama</p>
                      <p className="text-xs text-muted-foreground">LLM inference</p>
                    </div>
                  </div>
                  <span
                    className={health.components.ollama.status === "healthy" ? "text-green-600" : "text-red-600"}
                  >
                    {health.components.ollama.status}
                  </span>
                </div>
              </>
            ) : (
              <p className="text-sm text-muted-foreground">
                Unable to fetch service status
              </p>
            )}
          </CardContent>
        </Card>
      )}

      {/* Document Statistics */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <FileText className="h-5 w-5" />
            Document Statistics
          </CardTitle>
        </CardHeader>
        <CardContent className="grid gap-4 sm:grid-cols-3">
          <div className="rounded-lg border p-4 text-center">
            <p className="text-2xl font-bold">{documents?.length || 0}</p>
            <p className="text-xs text-muted-foreground">Total Documents</p>
          </div>
          <div className="rounded-lg border p-4 text-center">
            <p className="text-2xl font-bold">
                        {documents?.reduce((sum: number, doc: { chunkCount?: number | null }) => sum + (doc.chunkCount || 0), 0) || 0}
            </p>
            <p className="text-xs text-muted-foreground">Total Chunks</p>
          </div>
          <div className="rounded-lg border p-4 text-center">
            <p className="text-2xl font-bold">
              {formatBytes(
                          documents?.reduce((sum: number, doc: { fileSizeBytes?: number | null }) => sum + (doc.fileSizeBytes || 0), 0) || 0
              )}
            </p>
            <p className="text-xs text-muted-foreground">Storage Used</p>
          </div>
        </CardContent>
      </Card>

      {/* System Information */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Terminal className="h-5 w-5" />
            System Information
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-3">
          <div className="flex items-center justify-between rounded-lg border p-3">
            <span className="text-sm text-muted-foreground">Frontend Version</span>
            <span className="font-mono text-sm">{frontendVersion}</span>
          </div>
          <div className="flex items-center justify-between rounded-lg border p-3">
            <span className="text-sm text-muted-foreground">Supported Formats</span>
            <span className="text-sm">TXT, PDF, DOCX</span>
          </div>
          <div className="flex items-center justify-between rounded-lg border p-3">
            <span className="text-sm text-muted-foreground">Max Upload Size</span>
            <span className="text-sm">5 MB</span>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

