export type CurrentUser = { email: string; role: 'Student' | 'Admin' }

export type AuthResponse = {
  user: CurrentUser
  csrfToken: string
  accessToken: string
}

export type CreateIdResponse = { id: number }

export type TaskSummary = {
  id: number
  title: string
  category: string
  isCompleted: boolean
  topicId?: number | null
  topicTitle?: string | null
}

export type RankBadge = {
  key: string
  label: string
  tone: string
}

export type StudentProgress = {
  completedCount: number
  totalTasks: number
  percent: number
  badge: RankBadge
}

export type TopicSummary = {
  id: number
  title: string
  slug: string
  isCompleted: boolean
}

export type TopicTaskSummary = {
  id: number
  title: string
  isCompleted: boolean
}

export type TopicDetails = {
  id: number
  title: string
  slug: string
  descriptionMarkdown: string
  tasks: TopicTaskSummary[]
}

export type AdminTopicSummary = {
  id: number
  title: string
  slug: string
  isPublished: boolean
  createdAtUtc: string
  publishedAtUtc?: string | null
  taskCount: number
}

export type AdminTopicUpsert = {
  title: string
  slug: string
  descriptionMarkdown: string
  isPublished: boolean
}

export type AdminTopicTaskUpsert = {
  id?: number | null
  title: string
  descriptionMarkdown: string
  starterSql: string
  seedSql: string
  expectedSql: string
  sqlMode: 'SelectOnly' | 'SandboxSafe'
  isPublished: boolean
}

export type AdminTopicWithTasks = {
  id: number
  title: string
  slug: string
  descriptionMarkdown: string
  isPublished: boolean
  tasks: AdminTopicTaskUpsert[]
}

export type AdminTaskSummary = {
  id: number
  title: string
  category: string
  isPublished: boolean
  createdAtUtc: string
  publishedAtUtc?: string | null
}

export type TaskDetails = {
  id: number
  title: string
  category: string
  descriptionMarkdown: string
  starterSql: string
  sqlMode: 'SelectOnly' | 'SandboxSafe'
}

export type AdminTaskCreate = {
  category: string
  title: string
  descriptionMarkdown: string
  starterSql: string
  seedSql: string
  expectedSql: string
  sqlMode: 'SelectOnly' | 'SandboxSafe'
  isPublished: boolean
}

export type AdminTaskUpdate = AdminTaskCreate

export type ColumnSchema = {
  name: string
  dataType: string
  isNullable: boolean
  isPrimaryKey?: boolean
  isForeignKey?: boolean
  isIndexed?: boolean
}

export type TableSchema = {
  name: string
  columns: ColumnSchema[]
}

export type TaskSchemaResponse = {
  tables: TableSchema[]
}

export type PreviewColumn = {
  name: string
  isPrimaryKey?: boolean
  isForeignKey?: boolean
  isIndexed?: boolean
}

export type TablePreview = {
  name: string
  columns: PreviewColumn[]
  rows: Record<string, any>[]
}

export type TaskPreviewResponse = {
  tables: TablePreview[]
}

export type RunResult = {
  ok: boolean
  message?: string
  rows?: any[]
  columns?: string[]
  expectedRows?: any[]
  expectedColumns?: string[]
  matches?: boolean
}

export type SubmissionDto = {
  id: number
  taskId: number
  taskTitle: string
  studentEmail: string
  submittedAtUtc: string
  sql: string
  isCorrect?: boolean | null
  runnerMessage?: string | null
}

export type ExpectedResultResponse = {
  rows: Record<string, any>[]
}