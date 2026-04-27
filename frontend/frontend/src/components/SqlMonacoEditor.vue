<template>
  <div class="border border-slate-800 rounded-xl overflow-hidden bg-slate-950/30">
    <div ref="el" class="h-[260px] sm:h-[320px]" />
  </div>
</template>

<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref, watch } from 'vue'
import type { TableSchema } from '@/types/api'
import type * as Monaco from 'monaco-editor'

const props = defineProps<{
  modelValue: string
  schema?: TableSchema[]
  readOnly?: boolean
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', v: string): void
}>()

const el = ref<HTMLDivElement | null>(null)
let monaco: typeof import('monaco-editor') | null = null
let editor: Monaco.editor.IStandaloneCodeEditor | null = null
let providerDispose: Monaco.IDisposable | null = null

function buildCompletionItems(schema: TableSchema[] | undefined): Monaco.languages.CompletionItem[]
{
  const items: Monaco.languages.CompletionItem[] = []

  // Alap SQL kulcsszavak
  const keywords = [
    'SELECT', 'FROM', 'WHERE', 'GROUP BY', 'ORDER BY', 'HAVING', 'LIMIT',
    'JOIN', 'LEFT JOIN', 'RIGHT JOIN', 'INNER JOIN', 'ON',
    'COUNT', 'SUM', 'AVG', 'MIN', 'MAX',
    'DISTINCT', 'AS'
  ]
  for (const k of keywords) {
    items.push({
      label: k,
      kind: (monaco as any).languages.CompletionItemKind.Keyword,
      insertText: k,
      range: undefined as any
    })
  }

  if (!schema) return items

  for (const t of schema) {
    items.push({
      label: t.name,
      kind: (monaco as any).languages.CompletionItemKind.Struct,
      insertText: `\`${t.name}\``,
      detail: 'tábla',
      range: undefined as any
    })

    for (const c of t.columns) {
      items.push({
        label: `${t.name}.${c.name}`,
        kind: (monaco as any).languages.CompletionItemKind.Field,
        insertText: `\`${t.name}\`.\`${c.name}\``,
        detail: c.dataType,
        range: undefined as any
      })
      items.push({
        label: c.name,
        kind: (monaco as any).languages.CompletionItemKind.Field,
        insertText: `\`${c.name}\``,
        detail: `${t.name} · ${c.dataType}`,
        range: undefined as any
      })
    }
  }
  return items
}

function installCompletionProvider() {
  if (!monaco) return
  providerDispose?.dispose()

  const baseItems = buildCompletionItems(props.schema)

  providerDispose = monaco.languages.registerCompletionItemProvider('sql', {
    triggerCharacters: ['.', ' ', '`'],
    provideCompletionItems: (model, position) => {
      const word = model.getWordUntilPosition(position)
      const range = {
        startLineNumber: position.lineNumber,
        endLineNumber: position.lineNumber,
        startColumn: word.startColumn,
        endColumn: word.endColumn
      }
      const suggestions = baseItems.map((i) => ({ ...i, range }))
      return { suggestions }
    }
  })
}

onMounted(async () => {
  if (!el.value) return

  // Krumpli gépbarát: a Monaco csak akkor töltődik be, amikor az editor tényleg mountol.
  // Így a kezdeti UI gyorsabban megjelenik.
  monaco = await import('monaco-editor')

  editor = monaco.editor.create(el.value, {
    value: props.modelValue || '',
    language: 'sql',
    theme: 'vs-dark',
    fontSize: 12,
    fontLigatures: true,
    minimap: { enabled: false },
    scrollBeyondLastLine: false,
    wordWrap: 'on',
    automaticLayout: true,
    readOnly: props.readOnly ?? false,
    suggest: {
      showKeywords: true,
      showSnippets: true
    }
  })

  installCompletionProvider()

  editor.onDidChangeModelContent(() => {
    const v = editor?.getValue() ?? ''
    emit('update:modelValue', v)
  })
})

watch(
  () => props.modelValue,
  (v) => {
    if (!editor) return
    const current = editor.getValue()
    if (v !== current) editor.setValue(v)
  }
)

watch(
  () => props.schema,
  () => {
    if (!editor) return
    installCompletionProvider()
  }
)

watch(
  () => props.readOnly,
  (ro) => {
    editor?.updateOptions({ readOnly: ro ?? false })
  }
)

onBeforeUnmount(() => {
  providerDispose?.dispose()
  editor?.dispose()
  editor = null
  monaco = null
})
</script>
