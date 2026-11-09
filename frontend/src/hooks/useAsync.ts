import { useCallback, useEffect, useState } from 'react'

interface AsyncState<T> {
  data: T | undefined
  error: Error | undefined
  loading: boolean
  /** Runs the request again, e.g. after a mutation. */
  reload: () => void
}

/**
 * Runs an async function whenever `deps` change and tracks loading/error/data.
 * If deps change before a request finishes, the stale result is ignored, so a slow
 * response for an old filter can't overwrite the newer one.
 */
export function useAsync<T>(fn: () => Promise<T>, deps: readonly unknown[]): AsyncState<T> {
  const [data, setData] = useState<T>()
  const [error, setError] = useState<Error>()
  const [loading, setLoading] = useState(true)
  const [version, setVersion] = useState(0)

  // eslint-disable-next-line react-hooks/exhaustive-deps -- callers pass the deps that `fn` closes over
  const run = useCallback(fn, deps)

  useEffect(() => {
    let current = true
    setLoading(true)
    setError(undefined)

    run()
      .then((result) => current && setData(result))
      .catch((err: unknown) => current && setError(err instanceof Error ? err : new Error(String(err))))
      .finally(() => current && setLoading(false))

    return () => {
      current = false
    }
  }, [run, version])

  const reload = useCallback(() => setVersion((v) => v + 1), [])

  return { data, error, loading, reload }
}
