import { useEffect, useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import './App.css'

type AnalyzedFolder = {
  path: string
  lastAnalyzedAt: string
  fileCount: number
}

type FileVersion = {
  path: string
  version: number
}

type AnalysisResult = {
  path: string
  analyzedAt: string
  isInitialSnapshot: boolean
  newFiles: FileVersion[]
  changedFiles: FileVersion[]
  removedFiles: FileVersion[]
  removedDirectories: string[]
  currentFiles: FileVersion[]
}

function App() {
  const [path, setPath] = useState('')
  const [folders, setFolders] = useState<AnalyzedFolder[]>([])
  const [result, setResult] = useState<AnalysisResult | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [isLoadingFolders, setIsLoadingFolders] = useState(true)
  const [analyzingPath, setAnalyzingPath] = useState<string | null>(null)

  useEffect(() => {
    void loadFolders()
  }, [])

  const hasResultChanges = useMemo(() => {
    if (!result) {
      return false
    }

    return (
      result.newFiles.length > 0 ||
      result.changedFiles.length > 0 ||
      result.removedFiles.length > 0 ||
      result.removedDirectories.length > 0
    )
  }, [result])

  async function loadFolders() {
    setIsLoadingFolders(true)
    setError(null)

    try {
      const response = await fetch('/api/folders')
      if (!response.ok) {
        throw new Error('Nepodařilo se načíst uložené složky.')
      }

      setFolders(await response.json())
    } catch (loadError) {
      setError(getErrorMessage(loadError))
    } finally {
      setIsLoadingFolders(false)
    }
  }

  async function analyzeFolder(folderPath: string) {
    const trimmedPath = folderPath.trim()
    if (!trimmedPath) {
      setError('Zadejte cestu ke složce.')
      return
    }

    setAnalyzingPath(trimmedPath)
    setError(null)

    try {
      const response = await fetch('/api/folders/analyze', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ path: trimmedPath }),
      })

      if (!response.ok) {
        const problem = await response.json().catch(() => null)
        throw new Error(problem?.message ?? 'Analýza složky selhala.')
      }

      setResult(await response.json())
      await loadFolders()
    } catch (analyzeError) {
      setError(getErrorMessage(analyzeError))
    } finally {
      setAnalyzingPath(null)
    }
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    void analyzeFolder(path)
  }

  return (
    <main className="app-shell">
      <section className="border-bottom bg-white">
        <div className="container py-4 py-lg-5">
          <div className="row align-items-end g-4">
            <div className="col-lg-7">
              <h1 className="h2 mb-2">Detekce změn v lokálních složkách</h1>
              <p className="text-secondary mb-0">
                Analýza běží ručně na backendu přes System.IO a ukládá JSON snapshot pro každou složku.
              </p>
            </div>
            <div className="col-lg-5">
              <form className="d-flex gap-2" onSubmit={handleSubmit}>
                <input
                  className="form-control"
                  value={path}
                  onChange={(event) => setPath(event.target.value)}
                  placeholder="C:\Projects\UkazkovaSlozka"
                  aria-label="Cesta ke složce"
                />
                <button className="btn btn-primary text-nowrap" disabled={analyzingPath !== null} type="submit">
                  Analyzovat
                </button>
              </form>
            </div>
          </div>
        </div>
      </section>

      <section className="container py-4">
        {error && (
          <div className="alert alert-danger" role="alert">
            {error}
          </div>
        )}

        <div className="row g-4">
          <div className="col-lg-4">
            <section className="panel">
              <div className="d-flex justify-content-between align-items-center mb-3">
                <h2 className="h5 mb-0">Dříve analyzované složky</h2>
                <button className="btn btn-outline-secondary btn-sm" onClick={() => void loadFolders()} type="button">
                  Obnovit
                </button>
              </div>

              {isLoadingFolders ? (
                <div className="text-secondary">Načítám složky...</div>
              ) : folders.length === 0 ? (
                <div className="text-secondary">Zatím není uložený žádný snapshot.</div>
              ) : (
                <div className="folder-list">
                  {folders.map((folder) => (
                    <div className="folder-row" key={folder.path}>
                      <div className="min-width-0">
                        <div className="folder-path">{folder.path}</div>
                        <div className="small text-secondary">
                          {folder.fileCount} souborů, poslední analýza {formatDate(folder.lastAnalyzedAt)}
                        </div>
                      </div>
                      <button
                        className="btn btn-outline-primary btn-sm text-nowrap"
                        disabled={analyzingPath !== null}
                        onClick={() => void analyzeFolder(folder.path)}
                        type="button"
                      >
                        Znovu
                      </button>
                    </div>
                  ))}
                </div>
              )}
            </section>
          </div>

          <div className="col-lg-8">
            <section className="panel">
              {!result ? (
                <div className="empty-state">
                  <h2 className="h5">Výsledek analýzy</h2>
                  <p className="text-secondary mb-0">Zadejte novou cestu nebo spusťte analýzu uložené složky.</p>
                </div>
              ) : (
                <>
                  <div className="d-flex flex-wrap justify-content-between gap-3 mb-4">
                    <div className="min-width-0">
                      <h2 className="h5 mb-1">Výsledek analýzy</h2>
                      <div className="folder-path">{result.path}</div>
                      <div className="small text-secondary">{formatDate(result.analyzedAt)}</div>
                    </div>
                    {result.isInitialSnapshot ? (
                      <span className="badge text-bg-info align-self-start">První snapshot</span>
                    ) : hasResultChanges ? (
                      <span className="badge text-bg-warning align-self-start">Změny nalezeny</span>
                    ) : (
                      <span className="badge text-bg-success align-self-start">Beze změn</span>
                    )}
                  </div>

                  {result.isInitialSnapshot && (
                    <div className="alert alert-info">
                      Pro tuto složku byl uložen první snapshot. Soubory se proto nevypisují jako nové.
                    </div>
                  )}

                  <div className="result-grid">
                    <FileList title="Nové soubory" files={result.newFiles} emptyText="Žádné nové soubory." />
                    <FileList
                      title="Změněné soubory"
                      files={result.changedFiles}
                      emptyText="Žádné změněné soubory."
                    />
                    <FileList
                      title="Odstraněné soubory"
                      files={result.removedFiles}
                      emptyText="Žádné odstraněné soubory."
                    />
                    <DirectoryList
                      title="Odstraněné podadresáře"
                      directories={result.removedDirectories}
                      emptyText="Žádné odstraněné podadresáře."
                    />
                  </div>

                  <div className="mt-4">
                    <FileList
                      title="Aktuální soubory a verze"
                      files={result.currentFiles}
                      emptyText="Složka neobsahuje žádné soubory."
                      isWide
                    />
                  </div>
                </>
              )}
            </section>
          </div>
        </div>
      </section>
    </main>
  )
}

type FileListProps = {
  title: string
  files: FileVersion[]
  emptyText: string
  isWide?: boolean
}

function FileList({ title, files, emptyText, isWide = false }: FileListProps) {
  return (
    <section className={isWide ? 'result-block result-block-wide' : 'result-block'}>
      <h3 className="h6 mb-3">{title}</h3>
      {files.length === 0 ? (
        <div className="small text-secondary">{emptyText}</div>
      ) : (
        <div className="table-responsive">
          <table className="table table-sm align-middle mb-0">
            <thead>
              <tr>
                <th scope="col">Soubor</th>
                <th className="version-cell" scope="col">
                  Verze
                </th>
              </tr>
            </thead>
            <tbody>
              {files.map((file) => (
                <tr key={file.path}>
                  <td className="path-cell">{file.path}</td>
                  <td className="version-cell">{file.version}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  )
}

type DirectoryListProps = {
  title: string
  directories: string[]
  emptyText: string
}

function DirectoryList({ title, directories, emptyText }: DirectoryListProps) {
  return (
    <section className="result-block">
      <h3 className="h6 mb-3">{title}</h3>
      {directories.length === 0 ? (
        <div className="small text-secondary">{emptyText}</div>
      ) : (
        <ul className="list-group list-group-flush">
          {directories.map((directory) => (
            <li className="list-group-item px-0 py-2 path-cell" key={directory}>
              {directory}
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('cs-CZ', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value))
}

function getErrorMessage(error: unknown) {
  return error instanceof Error ? error.message : 'Nastala neočekávaná chyba.'
}

export default App
