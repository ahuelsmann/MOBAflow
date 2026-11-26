import { useId, useMemo } from 'react';
import { defaultSources, parseExamples } from '../../examples/loader';
import texts from '../../examples/texts.json';
import type { WorkflowNode, WorkflowNodeState, WorkflowNodeType } from '../../examples/types';
import { InertButton } from '../../shell/inert/InertButton';
import './workflows.css';

const NODE_WIDTH = 156;
const NODE_HEIGHT = 136;
const GRAPH_WIDTH = 528;
const GRAPH_HEIGHT = 336;

const NODE_TYPE_LABELS: Readonly<Record<WorkflowNodeType, string>> = {
  trigger: 'Trigger',
  action: 'Action',
  terminate: 'End',
};

const NODE_STATE_LABELS: Readonly<Record<WorkflowNodeState, string>> = {
  idle: 'Idle',
  completed: 'Completed',
};

function NodeTypeIcon({ type }: Readonly<{ type: WorkflowNodeType }>) {
  if (type === 'trigger') {
    return (
      <svg className="workflows-icon" viewBox="0 0 16 16" aria-hidden="true">
        <path d="M9.5 1.5 3.5 9h4l-1 5.5 6-7.5h-4z" fill="currentColor" />
      </svg>
    );
  }
  if (type === 'terminate') {
    return (
      <svg className="workflows-icon" viewBox="0 0 16 16" aria-hidden="true">
        <circle cx="8" cy="8" r="6" fill="none" stroke="currentColor" strokeWidth="1.5" />
        <path d="m5.25 8.25 1.75 1.75 3.75-4" fill="none" stroke="currentColor" strokeWidth="1.5" />
      </svg>
    );
  }
  return (
    <svg className="workflows-icon" viewBox="0 0 16 16" aria-hidden="true">
      <rect x="2.5" y="3.5" width="11" height="9" rx="1.5" fill="none" stroke="currentColor" strokeWidth="1.5" />
      <path d="M5 6.5h6M5 9.5h4" fill="none" stroke="currentColor" strokeWidth="1.5" />
    </svg>
  );
}

function NodeStateIcon({ state }: Readonly<{ state: WorkflowNodeState }>) {
  if (state === 'completed') {
    return (
      <svg className="workflows-icon" viewBox="0 0 16 16" aria-hidden="true">
        <path d="m3.5 8.5 3 3 6-6.5" fill="none" stroke="currentColor" strokeWidth="1.75" />
      </svg>
    );
  }
  return (
    <svg className="workflows-icon" viewBox="0 0 16 16" aria-hidden="true">
      <circle cx="8" cy="8" r="5" fill="none" stroke="currentColor" strokeWidth="1.5" />
      <circle cx="8" cy="8" r="1.5" fill="currentColor" />
    </svg>
  );
}

function getEdgeEndpoints(from: WorkflowNode, to: WorkflowNode) {
  if (to.position.x === from.position.x && to.position.y > from.position.y) {
    const cx = from.position.x + NODE_WIDTH / 2;
    return { x1: cx, y1: from.position.y + NODE_HEIGHT, x2: cx, y2: to.position.y, vertical: true };
  }
  return {
    x1: from.position.x + NODE_WIDTH,
    y1: from.position.y + NODE_HEIGHT / 2,
    x2: to.position.x,
    y2: to.position.y + NODE_HEIGHT / 2,
    vertical: false,
  };
}

function buildEdgePath(from: WorkflowNode, to: WorkflowNode): string {
  const { x1, y1, x2, y2, vertical } = getEdgeEndpoints(from, to);
  if (vertical) {
    const midY = Math.round((y1 + y2) / 2);
    return `M ${x1} ${y1} C ${x1} ${midY}, ${x2} ${midY}, ${x2} ${y2}`;
  }
  const offset = Math.max(12, Math.round(Math.abs(x2 - x1) * 0.6));
  return `M ${x1} ${y1} C ${x1 + offset} ${y1}, ${x2 - offset} ${y2}, ${x2} ${y2}`;
}

export function WorkflowsPage() {
  const workflow = useMemo(() => parseExamples(defaultSources).workflow, []);
  const searchHintId = useId();
  const dotPatternId = useId();
  const arrowMarkerId = useId();
  const propertyPrefixId = useId();

  const nodesById = useMemo(
    () => new Map(workflow.nodes.map((node) => [node.id, node])),
    [workflow.nodes],
  );

  return (
    <div className="workflows-page">
      <div className="workflows-header">
        <h1>Workflows</h1>
      </div>

      <div className="workflows-panes">
        <section className="workflows-pane workflows-list-pane" aria-label="Workflows list">
          <div className="workflows-pane-header">
            <h2>Workflows</h2>
            <fieldset className="workflows-pane-actions" aria-label="Workflow list actions">
              <InertButton className="workflows-icon-button" label="New workflow">
                <svg className="workflows-icon" viewBox="0 0 16 16" aria-hidden="true">
                  <path d="M8 2.5v11M2.5 8h11" fill="none" stroke="currentColor" strokeWidth="1.5" />
                </svg>
              </InertButton>
              <InertButton className="workflows-icon-button" label="Delete workflow">
                <svg className="workflows-icon" viewBox="0 0 16 16" aria-hidden="true">
                  <path d="M3 4.5h10M6 4.5V3h4v1.5M4.5 4.5l.5 8.5h6l.5-8.5" fill="none" stroke="currentColor" strokeWidth="1.5" />
                </svg>
              </InertButton>
            </fieldset>
          </div>

          <div className="workflows-search">
            <input
              type="search"
              className="workflows-field"
              aria-label="Search workflows"
              aria-disabled="true"
              aria-describedby={searchHintId}
              placeholder="Search workflows..."
              value=""
              readOnly
            />
            <span id={searchHintId} className="workflows-search-hint">
              {texts.inertHint}
            </span>
          </div>

          <ul className="workflows-list" aria-label="Workflow library">
            {workflow.list.map((item) => {
              const selected = item.id === workflow.selectedId;
              return (
                <li key={item.id}>
                  <InertButton className="workflows-list-item" pressed={selected}>
                    <svg className="workflows-icon" viewBox="0 0 16 16" aria-hidden="true">
                      <path d="M2.5 4.5h4v3h-4zM9.5 2.5h4v3h-4zM9.5 10.5h4v3h-4zM6.5 6h1.5v-2h1.5M8 6v6h1.5" fill="none" stroke="currentColor" strokeWidth="1.4" />
                    </svg>
                    <span className="workflows-list-item-name">{item.name}</span>
                  </InertButton>
                </li>
              );
            })}
          </ul>
        </section>

        <section className="workflows-pane workflows-actions-pane" aria-label="Actions">
          <div className="workflows-pane-header">
            <h2>Actions</h2>
            <fieldset className="workflows-view-toggle" aria-label="View mode">
              <InertButton className="workflows-toolbar-button" pressed={false}>
                List view
              </InertButton>
              <InertButton className="workflows-toolbar-button" pressed={true}>
                Graph view
              </InertButton>
            </fieldset>
          </div>

          <fieldset className="workflows-toolbar" aria-label="Workflow graph toolbar">
            <InertButton className="workflows-toolbar-button">Add action</InertButton>
            <InertButton className="workflows-toolbar-button">Run</InertButton>
            <InertButton className="workflows-toolbar-button">Validate</InertButton>
            <InertButton className="workflows-toolbar-button">Auto layout</InertButton>
            <InertButton className="workflows-toolbar-button">Zoom out</InertButton>
            <InertButton className="workflows-toolbar-button">100%</InertButton>
            <InertButton className="workflows-toolbar-button">Zoom in</InertButton>
          </fieldset>

          <div className="workflows-graph-viewport">
            <section className="workflows-graph-canvas" aria-label="Workflow graph">
              <svg className="workflows-graph-svg" viewBox={`0 0 ${GRAPH_WIDTH} ${GRAPH_HEIGHT}`} aria-hidden="true">
                <defs>
                  <pattern id={dotPatternId} width="24" height="24" patternUnits="userSpaceOnUse">
                    <circle cx="12" cy="12" r="1" className="workflows-grid-dot" />
                  </pattern>
                  <marker
                    id={arrowMarkerId}
                    viewBox="0 0 10 10"
                    refX="9"
                    refY="5"
                    markerWidth="6"
                    markerHeight="6"
                    orient="auto"
                  >
                    <path d="M 0 1.5 L 9 5 L 0 8.5 z" className="workflows-edge-arrow" />
                  </marker>
                </defs>
                <rect width={GRAPH_WIDTH} height={GRAPH_HEIGHT} fill={`url(#${dotPatternId})`} />
                {workflow.edges.map((edge) => {
                  const fromNode = nodesById.get(edge.from);
                  const toNode = nodesById.get(edge.to);
                  if (!fromNode || !toNode) return null;
                  return (
                    <path
                      key={`${edge.from}->${edge.to}`}
                      className="workflows-edge-path"
                      d={buildEdgePath(fromNode, toNode)}
                      markerEnd={`url(#${arrowMarkerId})`}
                      data-edge-from={edge.from}
                      data-edge-to={edge.to}
                    />
                  );
                })}
                {workflow.edges.map((edge) => {
                  const fromNode = nodesById.get(edge.from);
                  const toNode = nodesById.get(edge.to);
                  if (!fromNode || !toNode) return null;
                  const { x1, y1, x2, y2 } = getEdgeEndpoints(fromNode, toNode);
                  return (
                    <g key={`ports-${edge.from}->${edge.to}`}>
                      <circle className="workflows-port-dot" cx={x1} cy={y1} r="4" />
                      <circle className="workflows-port-dot" cx={x2} cy={y2} r="4" />
                    </g>
                  );
                })}
              </svg>

              <ol className="workflows-nodes-list" aria-label="Workflow nodes">
                {workflow.nodes.map((node) => {
                  const typeLabel = NODE_TYPE_LABELS[node.type];
                  const stateLabel = NODE_STATE_LABELS[node.state];
                  return (
                    <li
                      key={node.id}
                      className={`workflows-node-slot workflows-node-pos-${node.id}`}
                      data-node-id={node.id}
                      data-node-step={node.step}
                      data-node-state={node.state}
                    >
                      <InertButton className={`workflows-node-card workflows-node-type-${node.type}`}>
                        <span className="workflows-node-meta">
                          <span className="workflows-node-type">
                            <NodeTypeIcon type={node.type} />
                            <span>{typeLabel}</span>
                          </span>
                          <span className="workflows-node-step">Step {node.step}</span>
                        </span>
                        <span className="workflows-node-title">{node.name}</span>
                        <span className="workflows-node-summary">{node.summary}</span>
                        <span className="workflows-node-footer">
                          <span className={`workflows-node-state workflows-state-${node.state}`}>
                            <NodeStateIcon state={node.state} />
                            <span>{stateLabel}</span>
                          </span>
                        </span>
                      </InertButton>
                    </li>
                  );
                })}
              </ol>
            </section>
          </div>
        </section>

        <section className="workflows-pane workflows-properties-pane" aria-label="Properties">
          <div className="workflows-pane-header">
            <h2>Properties</h2>
          </div>

          <p className="workflows-description">{workflow.description}</p>

          <div className="workflows-properties-fields">
            {workflow.properties.map((property, index) => {
              const fieldId = `${propertyPrefixId}-prop-${index}`;
              return (
                <div key={property.label} className="workflows-property-field">
                  <label htmlFor={fieldId}>{property.label}</label>
                  {property.value.length > 30 ? (
                    <textarea
                      id={fieldId}
                      className="workflows-field workflows-property-input workflows-property-textarea"
                      readOnly
                      rows={3}
                      value={property.value}
                    />
                  ) : (
                    <input
                      id={fieldId}
                      type="text"
                      className="workflows-field workflows-property-input"
                      readOnly
                      value={property.value}
                    />
                  )}
                </div>
              );
            })}
          </div>
        </section>
      </div>
    </div>
  );
}
