import { useQuery } from "@tanstack/react-query";
import { apiClient } from "../api/client";
import { Card, CardHeader, CardTitle, CardContent, CardDescription } from "../components/common/Card";
import { Button } from "../components/common/Button";
import { LoadingSpinner, LoadingCard } from "../components/common/Loading";
import { EmptyState } from "../components/common/EmptyState";
import { formatDate, formatBytes } from "../utils";
import {
  FileText,
  FileText as FileIcon,
  MessageSquare,
  Upload,
  RefreshCw,
  CheckCircle,
  AlertCircle,
  XCircle,
} from "lucide-react";

export function Dashboard() {
  const { data: health, isLoading: healthLoading } = useQuery({
    queryKey: ["health"],
    queryFn: () => apiClient.getHealth(),
    refetchInterval: 30000,
    retry: 1,
  });

  const { data: documents, isLoading: docsLoading } = useQuery({
    queryKey: ["documents"],
    queryFn: () => apiClient.getDocuments(),
    refetchOnWindowFocus: false,
  });

  // Calculate statistics
  const totalDocuments = documents?.length || 0;
    const totalChunks = documents?.reduce((sum: number, doc: { chunkCount?: number | null }) => sum + (doc.chunkCount || 0), 0) || 0;
    const fileTypesRecord = documents?.reduce((acc: Record<string, number>, doc: { contentType: string }) => {
    const type = doc.contentType.toLowerCase();
    if (type.includes("pdf")) acc.pdf = (acc.pdf || 0) + 1;
    else if (type.includes("word")) acc.docx = (acc.docx || 0) + 1;
    else if (type.includes("text")) acc.txt = (acc.txt || 0) + 1;
    return acc;
      }, { pdf: 0, docx: 0, txt: 0 }) || { pdf: 0, docx: 0, txt: 0 };
      const fileTypes = Object.entries(fileTypesRecord).map(([type, count]): { type: string; count: number } => ({ type, count: count as number }));
    const totalFileSize = documents?.reduce((sum: number, doc: { fileSizeBytes?: number | null }) => sum + (doc.fileSizeBytes || 0), 0) || 0;
  const recentDocuments = documents?.sort((a: any, b: any) => {
    const dateA = a.updatedAt ? new Date(a.updatedAt).getTime() : 0;
    const dateB = b.updatedAt ? new Date(b.updatedAt).getTime() : 0;
    return dateB - dateA;
  }).slice(0, 5) || [];

  const healthStatus = healthLoading ? "loading" : health?.status || "unknown";

  const getStatusIcon = (status: string) => {
    switch (status) {
      case "healthy":
      case "loading":
        return <CheckCircle className="h-6 w-6 text-green-600" />;
      case "degraded":
        return <AlertCircle className="h-6 w-6 text-yellow-600" />;
      default:
        return <XCircle className="h-6 w-6 text-red-600" />;
    }
  };

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div>
        <h1 className="text-2xl font-bold">Dashboard</h1>
        <p className="text-muted-foreground">Overview of your RAG system</p>
      </div>

      {/* Quick Actions */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        <Button variant="outline" className="justify-start" onClick={() => {}} disabled>
          <Upload className="mr-2 h-4 w-4" />
          Upload Document
        </Button>
        <Button variant="outline" className="justify-start" onClick={() => {}} disabled>
          <MessageSquare className="mr-2 h-4 w-4" />
          Start New Chat
        </Button>
        <Button variant="outline" className="justify-start" onClick={() => {}} disabled>
          <RefreshCw className="mr-2 h-4 w-4" />
          Refresh Data
        </Button>
      </div>

      {/* Statistics Cards */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-sm">
              <FileText className="h-4 w-4" />
              Total Documents
            </CardTitle>
          </CardHeader>
          <CardContent>
            {docsLoading ? (
                          <div><LoadingSpinner />
                          <p>Loading...</p></div>                        ) : (
                          <div className="text-3xl font-bold">{totalDocuments}</div>
                        )}
            <p className="mt-1 text-sm text-muted-foreground">
              {docsLoading ? "Loading..." : "All indexed documents"}
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-sm">
              <FileIcon className="h-4 w-4" />
              Total Chunks
            </CardTitle>
          </CardHeader>
          <CardContent>
            {docsLoading ? (
              <LoadingSpinner />
            ) : (
              <div className="text-3xl font-bold">{totalChunks}</div>
            )}
            <p className="mt-1 text-sm text-muted-foreground">
              {docsLoading ? "Loading..." : "Total text chunks"}
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-sm">
              <RefreshCw className="h-4 w-4" />
              Total Size
            </CardTitle>
          </CardHeader>
          <CardContent>
            {docsLoading ? (
              <LoadingSpinner />
            ) : (
              <div className="text-3xl font-bold">{formatBytes(totalFileSize)}</div>
            )}
            <p className="mt-1 text-sm text-muted-foreground">
              {docsLoading ? "Loading..." : "Total document storage"}
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-sm">
              {getStatusIcon(healthStatus)}
              Backend Status
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold capitalize">{healthStatus}</div>
            <p className="mt-1 text-sm text-muted-foreground">
              {healthStatus === "loading"
                ? "Checking..."
                : healthStatus === "healthy"
                  ? "All systems operational"
                  : "Some services are unavailable"}
            </p>
          </CardContent>
        </Card>
      </div>

      {/* File Type Breakdown */}
      {(!docsLoading || totalDocuments > 0) && (
        <Card>
          <CardHeader>
            <CardTitle>File Type Breakdown</CardTitle>
            <CardDescription>Documents by format</CardDescription>
          </CardHeader>
          <CardContent>
            {docsLoading ? (
              <div className="space-y-3">
                <LoadingCard className="h-8" />
                <LoadingCard className="h-8" />
                <LoadingCard className="h-8" />
              </div>
            ) : (
              <div className="space-y-2">
                            {fileTypes.map(({ type, count }) => (
                  <div key={type} className="flex items-center justify-between rounded-lg border p-3">
                    <div className="flex items-center gap-2">
                      {type === "txt" && <span className="text-xl">📄</span>}
                      {type === "pdf" && <span className="text-xl">📕</span>}
                      {type === "docx" && <span className="text-xl">📘</span>}
                      <span className="capitalize font-medium">{type}</span>
                    </div>
                    <div className="text-sm font-semibold">{count}</div>
                  </div>
                ))}
                {totalDocuments === 0 && (
                  <p className="text-center text-muted-foreground py-4">
                    No documents indexed yet
                  </p>
                )}
              </div>
            )}
          </CardContent>
        </Card>
      )}

      {/* Recent Documents */}
      <Card>
        <CardHeader>
          <CardTitle>Recent Documents</CardTitle>
          <CardDescription>Recently updated documents</CardDescription>
        </CardHeader>
        <CardContent>
          {docsLoading ? (
            <div className="space-y-3">
              <LoadingCard className="h-16" />
              <LoadingCard className="h-16" />
            </div>
          ) : recentDocuments.length === 0 ? (
            <EmptyState
              title="No recent documents"
              description="Upload a document to see it here"
              className="py-8"
            />
          ) : (
            <div className="space-y-3">
              {recentDocuments.map((doc: any) => (
                <div key={doc.documentId} className="flex items-center justify-between rounded-lg border p-3">
                  <div className="flex items-center gap-3">
                    <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-muted">
                      {doc.contentType.includes("pdf") ? (
                        <span className="text-xl">📕</span>
                      ) : doc.contentType.includes("word") ? (
                        <span className="text-xl">📘</span>
                      ) : (
                        <span className="text-xl">📄</span>
                      )}
                    </div>
                    <div>
                      <p className="font-medium">{doc.fileName}</p>
                      <p className="text-xs text-muted-foreground">
                        {doc.chunkCount} chunks • {formatBytes(doc.fileSizeBytes || 0)}
                      </p>
                    </div>
                  </div>
                  <div className="text-right">
                    <p className="text-sm font-medium">{formatDate(doc.updatedAt)}</p>
                    <p className="text-xs text-muted-foreground">
                      Updated
                    </p>
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}