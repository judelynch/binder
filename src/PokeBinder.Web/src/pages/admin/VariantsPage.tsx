import { VariantTypeManager } from '../../components/admin/VariantTypeManager'
import { PerCardVariantEditor } from '../../components/admin/PerCardVariantEditor'
import { BulkVariantAssignTool } from '../../components/admin/BulkVariantAssignTool'

export function VariantsPage() {
  return (
    <div className="space-y-5">
      <div className="grid grid-cols-1 gap-5 lg:grid-cols-2">
        <VariantTypeManager />
        <PerCardVariantEditor />
      </div>
      <BulkVariantAssignTool />
    </div>
  )
}
