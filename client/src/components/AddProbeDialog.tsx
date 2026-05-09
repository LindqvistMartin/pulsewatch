import {
  createContext,
  useContext,
  useState,
  type ReactNode,
} from 'react'
import { useForm, Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { toast } from 'sonner'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { useAppContext } from '@/contexts/AppContext'
import { useCreateProbe } from '@/api/hooks/useProbes'

// ─── Schema ──────────────────────────────────────────────────────────────────

const schema = z.object({
  name: z.string().min(1, 'Name is required').max(80),
  url: z.string().url('Must be a valid URL'),
  intervalSeconds: z.number().int().min(15, 'Minimum 15s').max(3600),
})

type FormValues = z.infer<typeof schema>

const INTERVALS = [
  { value: '15',   label: 'Every 15s' },
  { value: '30',   label: 'Every 30s' },
  { value: '60',   label: 'Every minute' },
  { value: '300',  label: 'Every 5 min' },
  { value: '900',  label: 'Every 15 min' },
  { value: '3600', label: 'Every hour' },
]

// ─── Context ──────────────────────────────────────────────────────────────────

interface AddProbeDialogContextValue {
  open: () => void
}

const AddProbeDialogContext = createContext<AddProbeDialogContextValue | null>(null)

export function useAddProbeDialog() {
  const ctx = useContext(AddProbeDialogContext)
  if (!ctx) throw new Error('useAddProbeDialog must be inside AddProbeDialogProvider')
  return ctx
}

// ─── Provider ────────────────────────────────────────────────────────────────

export function AddProbeDialogProvider({ children }: { children: ReactNode }) {
  const [isOpen, setIsOpen] = useState(false)

  return (
    <AddProbeDialogContext.Provider value={{ open: () => setIsOpen(true) }}>
      {children}
      <AddProbeDialogInner open={isOpen} onOpenChange={setIsOpen} />
    </AddProbeDialogContext.Provider>
  )
}

// ─── Dialog inner ────────────────────────────────────────────────────────────

interface AddProbeDialogInnerProps {
  open: boolean
  onOpenChange: (open: boolean) => void
}

function AddProbeDialogInner({ open, onOpenChange }: AddProbeDialogInnerProps) {
  const { selectedProjectId } = useAppContext()
  const createProbe = useCreateProbe(selectedProjectId ?? '')

  const {
    register,
    control,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { name: '', url: '', intervalSeconds: 30 },
  })

  function onSubmit(values: FormValues) {
    if (!selectedProjectId) return

    createProbe.mutate(
      { name: values.name, url: values.url, intervalSeconds: values.intervalSeconds },
      {
        onSuccess: () => {
          toast.success('Probe created')
          onOpenChange(false)
          // reset() called by handleClose when dialog closes
        },
        onError: (error: unknown) => {
          const msg =
            error instanceof Error ? error.message : 'Failed to create probe'
          toast.error(msg)
        },
      },
    )
  }

  function handleClose(val: boolean) {
    if (!val) {
      reset()
      createProbe.reset()
    }
    onOpenChange(val)
  }

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="text-sm font-medium">Add probe</DialogTitle>
        </DialogHeader>

        {!selectedProjectId ? (
          <p className="text-xs text-muted-foreground">Select a project first.</p>
        ) : (
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 pt-1">
            {/* Name */}
            <div className="space-y-1.5">
              <Label htmlFor="probe-name" className="text-xs">
                Name
              </Label>
              <Input
                id="probe-name"
                placeholder="My API"
                className="h-8 font-mono text-xs"
                {...register('name')}
              />
              {errors.name && (
                <p className="text-[10px] text-destructive">{errors.name.message}</p>
              )}
            </div>

            {/* URL */}
            <div className="space-y-1.5">
              <Label htmlFor="probe-url" className="text-xs">
                URL
              </Label>
              <Input
                id="probe-url"
                placeholder="https://api.example.com/health"
                className="h-8 font-mono text-xs"
                {...register('url')}
              />
              {errors.url && (
                <p className="text-[10px] text-destructive">{errors.url.message}</p>
              )}
            </div>

            {/* Interval */}
            <div className="space-y-1.5">
              <Label className="text-xs">Check interval</Label>
              <Controller
                name="intervalSeconds"
                control={control}
                render={({ field }) => (
                  <Select
                    value={String(field.value)}
                    onValueChange={val => field.onChange(Number(val))}
                  >
                    <SelectTrigger className="h-8 font-mono text-xs">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {INTERVALS.map(opt => (
                        <SelectItem key={opt.value} value={opt.value} className="font-mono text-xs">
                          {opt.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
              {errors.intervalSeconds && (
                <p className="text-[10px] text-destructive">{errors.intervalSeconds.message}</p>
              )}
            </div>

            {/* Actions */}
            <div className="flex justify-end gap-2 pt-2">
              <Button
                type="button"
                variant="ghost"
                size="sm"
                className="h-8 text-xs"
                onClick={() => handleClose(false)}
              >
                Cancel
              </Button>
              <Button
                type="submit"
                size="sm"
                className="h-8 font-mono text-xs"
                disabled={createProbe.isPending}
              >
                {createProbe.isPending ? 'Creating…' : 'Create probe'}
              </Button>
            </div>
          </form>
        )}
      </DialogContent>
    </Dialog>
  )
}
