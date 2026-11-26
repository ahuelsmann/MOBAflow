import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { fireEvent, render, screen, within } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { defaultSources, parseExamples } from '../../examples/loader';
import { WorkflowsPage } from './WorkflowsPage';

const workflow = parseExamples(defaultSources).workflow;
const hint = 'Available in the MOBAflow desktop app';

describe('WorkflowsPage (slice 17)', () => {
  it('renders the three panes with the selected workflow, connected graph nodes, and read-only properties (US2 S3, FR-037, FR-045)', () => {
    const { container } = render(<WorkflowsPage />);

    expect(screen.getByRole('heading', { level: 1, name: 'Workflows' })).toBeVisible();

    const listPane = screen.getByRole('region', { name: 'Workflows list' });
    const actionsPane = screen.getByRole('region', { name: 'Actions' });
    const propertiesPane = screen.getByRole('region', { name: 'Properties' });
    expect(listPane).toBeVisible();
    expect(actionsPane).toBeVisible();
    expect(propertiesPane).toBeVisible();

    const library = within(listPane).getByRole('list', { name: 'Workflow library' });
    for (const item of workflow.list) {
      const button = within(library).getByRole('button', { name: item.name });
      expect(button).toHaveAttribute('aria-disabled', 'true');
      expect(button).toHaveAttribute('aria-pressed', String(item.id === workflow.selectedId));
      expect(button).toHaveAccessibleDescription(hint);
    }

    expect(within(actionsPane).getByRole('button', { name: 'Graph view' })).toHaveAttribute('aria-pressed', 'true');
    expect(within(actionsPane).getByRole('button', { name: 'List view' })).toHaveAttribute('aria-pressed', 'false');

    const nodesList = within(actionsPane).getByRole('list', { name: 'Workflow nodes' });
    const nodeItems = within(nodesList).getAllByRole('listitem');
    expect(nodeItems).toHaveLength(workflow.nodes.length);

    const expectedTypeLabels = { trigger: 'Trigger', action: 'Action', terminate: 'End' } as const;
    const css = readFileSync(join(import.meta.dirname, 'workflows.css'), 'utf8');
    expect(css).toMatch(/\.workflows-graph-canvas\s*\{[^}]*block-size:\s*336px;\s*inline-size:\s*528px;/);
    expect(css).toMatch(/button\.workflows-node-card\s*\{[^}]*block-size:\s*136px;[^}]*inline-size:\s*156px;/);
    for (const node of workflow.nodes) {
      const slot = nodeItems.find((item) => item.getAttribute('data-node-id') === node.id);
      expect(slot).toBeDefined();
      expect(slot).toHaveClass(`workflows-node-pos-${node.id}`);
      const posMatch = new RegExp(
        String.raw`\.workflows-node-pos-${node.id}\s*\{[^}]*inset-block-start:\s*(\d+)px;\s*inset-inline-start:\s*(\d+)px;`,
      ).exec(css);
      expect(posMatch, node.id).not.toBeNull();
      expect(Number(posMatch![1])).toBe(node.position.y);
      expect(Number(posMatch![2])).toBe(node.position.x);
      expect(within(slot!).getByText(`Step ${node.step}`)).toBeVisible();
      const typeLabels = within(slot!).getAllByText(expectedTypeLabels[node.type]);
      expect(typeLabels[0]).toBeVisible();
      const titleMatches = within(slot!).getAllByText(node.name);
      expect(titleMatches.at(-1)).toBeVisible();
      expect(within(slot!).getByText(node.summary)).toBeVisible();
      expect(within(slot!).getByText('Idle')).toBeVisible();
      expect(slot!.querySelectorAll('svg').length).toBeGreaterThanOrEqual(2);

      const nodeButton = within(slot!).getByRole('button');
      expect(nodeButton).toHaveAttribute('aria-disabled', 'true');
      expect(nodeButton).toHaveAccessibleDescription(hint);
    }

    const svgEdges = actionsPane.querySelectorAll('svg.workflows-graph-svg path.workflows-edge-path');
    expect(svgEdges).toHaveLength(workflow.edges.length);
    workflow.edges.forEach((edge, index) => {
      expect(svgEdges[index]).toHaveAttribute('data-edge-from', edge.from);
      expect(svgEdges[index]).toHaveAttribute('data-edge-to', edge.to);
      expect(svgEdges[index]?.getAttribute('d')).toMatch(/^M \d+ \d+ C /);
    });

    expect(within(propertiesPane).getByText(workflow.description, { selector: 'p' })).toBeVisible();
    for (const prop of workflow.properties) {
      const input = within(propertiesPane).getByRole('textbox', { name: prop.label });
      expect(input).toHaveAttribute('readonly');
      expect(input).not.toBeDisabled();
      expect(input).toHaveValue(prop.value);
    }

    expect(container.querySelectorAll('[style]')).toHaveLength(0);
    const source = readFileSync(join(import.meta.dirname, 'WorkflowsPage.tsx'), 'utf8');
    expect(source).not.toMatch(/\bstyle=\s*\{/);
  });

  it('keeps every control on Workflows inert when activated (US2 S4, FR-035, SC-004)', () => {
    render(<WorkflowsPage />);

    const search = screen.getByRole('searchbox', { name: 'Search workflows' });
    expect(search).toHaveAttribute('readonly');
    expect(search).toHaveAttribute('aria-disabled', 'true');
    expect(search).toHaveAccessibleDescription(hint);

    const buttons = screen.getAllByRole('button');
    expect(buttons.length).toBeGreaterThanOrEqual(17);
    for (const button of buttons) {
      expect(button).toHaveAttribute('aria-disabled', 'true');
      expect(button).toHaveAccessibleDescription(hint);
      fireEvent.click(button);
    }

    expect(screen.getByRole('button', { name: 'Arrival Main Station' })).toHaveAttribute('aria-pressed', 'true');
    expect(screen.getByRole('button', { name: 'Departure Main Station' })).toHaveAttribute('aria-pressed', 'false');
    expect(screen.getByRole('button', { name: 'Shunting Announcement' })).toHaveAttribute('aria-pressed', 'false');
    expect(screen.getByRole('button', { name: 'Graph view' })).toHaveAttribute('aria-pressed', 'true');
    expect(screen.getByRole('button', { name: 'List view' })).toHaveAttribute('aria-pressed', 'false');
    for (const prop of workflow.properties) {
      expect(screen.getByRole('textbox', { name: prop.label })).toHaveValue(prop.value);
    }
  });
});
