export interface RagSource {
  source: string;
  chunkIndex: number;
  score: number;
}

export interface RagResponse {
  answer: string;
  sources: RagSource[];
}

export interface IndexedDocument {
  documentId: string;
  fileName: string;
  contentType: string;
  chunkCount: number;
  fileSizeBytes: number | null;
  createdAt: string | null;
  updatedAt: string | null;
}

export interface DocumentIngestionResult {
  documentId: string;
  fileName: string;
  chunkCount: number;
}

export interface DocumentUpdateResult {
  oldDocumentId: string;
  newDocumentId: string;
  fileName: string;
  chunkCount: number;
}

export interface RagRequest {
  conversationId: string;
  question: string;
}

export interface ConversationMessage {
  role: 'user' | 'assistant';
  content: string;
}

export interface Conversation {
  id: string;
  title?: string;
  lastMessage?: string;
  updatedAt?: string;
}

export interface HealthStatus {
  status: 'healthy' | 'degraded' | 'unhealthy';
  components: {
    ollama: { status: string; responseTimeMs?: number };
    qdrant: { status: string; responseTimeMs?: number };
  };
}

export interface ApiError {
  error: string;
  documentId?: string;
  fileName?: string;
}