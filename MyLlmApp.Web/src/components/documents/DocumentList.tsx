import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "../../api/client";
import { Button } from "../common/Button";
import { Input } from "../common/Input";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "../common/Dialog";
import { LoadingSpinner, LoadingCard } from "../common/Loading";
import { EmptyState } from "../common/EmptyState";
import type { IndexedDocument } from "../../types";
import {
  formatBytes,
  formatDate,
  getDocumentType,
} from "../../utils";
import {
  FileText,
  File,
  FilePlus,
  Upload,
  RefreshCw,
  Trash2,
  Replace,
  Search,
  Filter,
} from "lucide-react";

interface DocumentListProps {
  onUploadSuccess?: () => void;
}

export function DocumentList({ onUploadSuccess }: DocumentListProps) {
  const queryClient = useQueryClient();
  const [searchQuery, setSearchQuery] = useState("");
  const [sortBy, setSortBy] = useState<"name" | "date" | "size">("date");
  const [sortOrder, setSortOrder] = useState<"asc" | "desc">("desc");

  // Upload state
  const [isUploadOpen, setIsUploadOpen] = useState(false);
  const [uploadFile, setUploadFile] = useState<File | null>(null);
  const [uploadError, setUploadError] = useState<string | null>(null);
  const [isUploading, setIsUploading] = useState(false);

  // Replace state
  const [isReplaceOpen, setIsReplaceOpen] = useState(false);
  const [replaceDoc, setReplaceDoc] = useState<IndexedDocument | null>(null);
  const [replaceFile, setReplaceFile] = useState<File | null>(null);
  const [replaceError, setReplaceError] = useState<string | null>(null);
  const [isReplacing, setIsReplacing] = useState(false);

  // Delete state
  const [isDeleteOpen, setIsDeleteOpen] = useState(false);
  const [deleteDoc, setDeleteDoc] = useState<IndexedDocument | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);

  const { data: documents, isLoading } = useQuery({
    queryKey: ["documents"],
    queryFn: () => apiClient.getDocuments(),
  });

  const uploadMutation = useMutation({
    mutationFn: (file: File) => apiClient.uploadDocument(file),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["documents"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard"] });
      setIsUploadOpen(false);
      setUploadFile(null);
      setUploadError(null);
      onUploadSuccess?.();
    },
    onError: (error: unknown) => {
      const axiosError = error as { response?: { status?: number; data?: { error?: string } } };
      if (axiosError.response?.status === 409) {
        setUploadError("Document already exists. Please rename the file or delete the existing document.");
      } else {
        setUploadError(axiosError.response?.data?.error || "Upload failed");
      }
    },
  });

  const replaceMutation = useMutation({
    mutationFn: ({ documentId, file }: { documentId: string; file: File }) =>
      apiClient.replaceDocument(documentId, file),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["documents"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard"] });
      setIsReplaceOpen(false);
      setReplaceDoc(null);
      setReplaceFile(null);
      setReplaceError(null);
    },
    onError: (error: unknown) => {
      const axiosError = error as { response?: { status?: number; data?: { error?: string } } };
      if (axiosError.response?.status === 404) {
        setReplaceError("Document not found. It may have been deleted.");
      } else if (axiosError.response?.status === 409) {
        setReplaceError("A document with this content already exists.");
      } else {
        setReplaceError(axiosError.response?.data?.error || "Replace failed");
      }
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (documentId: string) => apiClient.deleteDocument(documentId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["documents"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard"] });
      setIsDeleteOpen(false);
      setDeleteDoc(null);
    },
    onError: (error: unknown) => {
      const axiosError = error as { response?: { status?: number; data?: { error?: string } } };
      if (axiosError.response?.status === 404) {
        setIsDeleteOpen(false);
        setDeleteDoc(null);
      }
    },
  });

  const handleUpload = async () => {
    if (!uploadFile) return;
    setIsUploading(true);
    setUploadError(null);
    await uploadMutation.mutateAsync(uploadFile);
    setIsUploading(false);
  };

  const handleReplace = async () => {
    if (!replaceDoc || !replaceFile) return;
    setIsReplacing(true);
    setReplaceError(null);
    await replaceMutation.mutateAsync({ documentId: replaceDoc.documentId, file: replaceFile });
    setIsReplacing(false);
  };

  const handleDelete = async () => {
    if (!deleteDoc) return;
    setIsDeleting(true);
    await deleteMutation.mutateAsync(deleteDoc.documentId);
    setIsDeleting(false);
  };

  const filteredDocuments = documents
    ? documents
        .filter((doc: IndexedDocument) =>
          doc.fileName.toLowerCase().includes(searchQuery.toLowerCase())
        )
        .sort((a: IndexedDocument, b: IndexedDocument) => {
          let cmp = 0;
          if (sortBy === "name") cmp = a.fileName.localeCompare(b.fileName);
          else if (sortBy === "date") {
            const dateA = a.updatedAt ? new Date(a.updatedAt).getTime() : 0;
            const dateB = b.updatedAt ? new Date(b.updatedAt).getTime() : 0;
            cmp = dateA - dateB;
          } else if (sortBy === "size") cmp = (a.fileSizeBytes || 0) - (b.fileSizeBytes || 0);
          return sortOrder === "asc" ? cmp : -cmp;
        })
    : [];

  const getFileIcon = (contentType: string) => {
    if (contentType.includes("pdf")) return <FileText className="h-5 w-5 text-red-500" />;
    if (contentType.includes("word") || contentType.includes("document"))
      return <FileText className="h-5 w-5 text-blue-500" />;
    return <File className="h-5 w-5 text-gray-500" />;
  };

  if (isLoading) {
    return (
      <div className="space-y-4">
        <LoadingCard />
        <LoadingCard />
        <LoadingCard />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold">Documents</h1>
          <p className="text-muted-foreground">Manage your indexed documents</p>
        </div>
        <Button onClick={() => setIsUploadOpen(true)}>
          <Upload className="mr-2 h-4 w-4" />
          Upload Document
        </Button>
      </div>

      {/* Filters */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center">
        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder="Search documents..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="pl-9"
          />
        </div>
        <div className="flex items-center gap-2">
          <Filter className="h-4 w-4 text-muted-foreground" />
          <select
            className="rounded-md border border-input bg-background px-3 py-2 text-sm"
            value={sortBy}
            onChange={(e) => setSortBy(e.target.value as "name" | "date" | "size")}
          >
            <option value="date">Date</option>
            <option value="name">Name</option>
            <option value="size">Size</option>
          </select>
          <Button
            variant="outline"
            size="sm"
            onClick={() => setSortOrder(sortOrder === "asc" ? "desc" : "asc")}
          >
            {sortOrder === "asc" ? "↑" : "↓"}
          </Button>
          <Button variant="outline" size="sm" onClick={() => queryClient.invalidateQueries({ queryKey: ["documents"] })}>
            <RefreshCw className="h-4 w-4" />
          </Button>
        </div>
      </div>

      {/* Document List */}
      {filteredDocuments.length === 0 ? (
        <EmptyState
          title="No documents found"
          description={searchQuery ? "Try adjusting your search" : "Upload your first document to get started"}
          icon={<FilePlus className="h-6 w-6" />}
          action={
            !searchQuery && (
              <Button onClick={() => setIsUploadOpen(true)}>
                <Upload className="mr-2 h-4 w-4" />
                Upload Document
              </Button>
            )
          }
        />
      ) : (
        <div className="rounded-md border">
          <table className="w-full">
            <thead>
              <tr className="border-b bg-muted/50 text-left text-sm">
                <th className="px-4 py-3 font-medium">Name</th>
                <th className="px-4 py-3 font-medium">Type</th>
                <th className="px-4 py-3 font-medium">Size</th>
                <th className="px-4 py-3 font-medium">Chunks</th>
                <th className="px-4 py-3 font-medium">Updated</th>
                <th className="px-4 py-3 font-medium text-right">Actions</th>
              </tr>
            </thead>
            <tbody>
              {filteredDocuments.map((doc: IndexedDocument) => (
                <tr key={doc.documentId} className="border-b last:border-0">
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-2">
                      {getFileIcon(doc.contentType)}
                      <span className="font-medium">{doc.fileName}</span>
                    </div>
                  </td>
                  <td className="px-4 py-3 text-muted-foreground">
                    {getDocumentType(doc.contentType)}
                  </td>
                  <td className="px-4 py-3 text-muted-foreground">
                    {formatBytes(doc.fileSizeBytes)}
                  </td>
                  <td className="px-4 py-3">{doc.chunkCount}</td>
                  <td className="px-4 py-3 text-muted-foreground">
                    {formatDate(doc.updatedAt)}
                  </td>
                  <td className="px-4 py-3 text-right">
                    <div className="flex items-center justify-end gap-1">
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => {
                          setReplaceDoc(doc);
                          setIsReplaceOpen(true);
                        }}
                        title="Replace"
                      >
                        <Replace className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => {
                          setDeleteDoc(doc);
                          setIsDeleteOpen(true);
                        }}
                        title="Delete"
                      >
                        <Trash2 className="h-4 w-4 text-destructive" />
                      </Button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Upload Dialog */}
      <Dialog open={isUploadOpen} onOpenChange={setIsUploadOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Upload Document</DialogTitle>
            <DialogDescription>
              Upload a TXT, PDF, or DOCX file. Maximum size: 5 MB.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <Input
              type="file"
              accept=".txt,.pdf,.docx"
              onChange={(e) => {
                const file = e.target.files?.[0];
                if (file && file.size > 5 * 1024 * 1024) {
                  setUploadError("File size exceeds 5 MB limit");
                  setUploadFile(null);
                } else {
                  setUploadError(null);
                  setUploadFile(file || null);
                }
              }}
            />
            {uploadFile && (
              <div className="text-sm text-muted-foreground">
                Selected: {uploadFile.name} ({formatBytes(uploadFile.size)})
              </div>
            )}
            {uploadError && (
              <p className="text-sm text-destructive">{uploadError}</p>
            )}
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setIsUploadOpen(false)}>
              Cancel
            </Button>
            <Button
              onClick={handleUpload}
              disabled={!uploadFile || isUploading}
            >
              {isUploading && <LoadingSpinner size="sm" className="mr-2" />}
              Upload
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Replace Dialog */}
      <Dialog open={isReplaceOpen} onOpenChange={setIsReplaceOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Replace Document</DialogTitle>
            <DialogDescription>
              Replace "{replaceDoc?.fileName}". This will re-index the document and may change
              its ID.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <Input
              type="file"
              accept=".txt,.pdf,.docx"
              onChange={(e) => {
                const file = e.target.files?.[0];
                if (file && file.size > 5 * 1024 * 1024) {
                  setReplaceError("File size exceeds 5 MB limit");
                  setReplaceFile(null);
                } else {
                  setReplaceError(null);
                  setReplaceFile(file || null);
                }
              }}
            />
            {replaceFile && (
              <div className="text-sm text-muted-foreground">
                Selected: {replaceFile.name} ({formatBytes(replaceFile.size)})
              </div>
            )}
            {replaceError && (
              <p className="text-sm text-destructive">{replaceError}</p>
            )}
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setIsReplaceOpen(false)}>
              Cancel
            </Button>
            <Button
              onClick={handleReplace}
              disabled={!replaceFile || isReplacing}
            >
              {isReplacing && <LoadingSpinner size="sm" className="mr-2" />}
              Replace
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Delete Dialog */}
      <Dialog open={isDeleteOpen} onOpenChange={setIsDeleteOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete Document</DialogTitle>
            <DialogDescription>
              Are you sure you want to delete "{deleteDoc?.fileName}"? This action cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setIsDeleteOpen(false)}>
              Cancel
            </Button>
            <Button variant="destructive" onClick={handleDelete} disabled={isDeleting}>
              {isDeleting && <LoadingSpinner size="sm" className="mr-2" />}
              Delete
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}