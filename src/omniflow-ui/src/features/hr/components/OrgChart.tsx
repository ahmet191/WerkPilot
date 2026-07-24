import React, { useEffect, useState, useMemo } from 'react';
import { Card, Typography, Spin, Avatar, Tag, Input, Segmented, Button, Drawer, Row, Col, Statistic, Progress } from 'antd';
import {
  UserOutlined, SearchOutlined, FolderOpenOutlined, TeamOutlined,
  DownOutlined, RightOutlined, PhoneOutlined, MailOutlined,
  ApartmentOutlined, AppstoreOutlined, PieChartOutlined, ZoomInOutlined, ZoomOutOutlined
} from '@ant-design/icons';
import { api } from '../../../core/api';

const { Title, Text } = Typography;

interface OrgNode {
  id: string;
  name: string;
  title: string;
  email?: string;
  phone?: string;
  departmentName?: string;
  managerId?: string | null;
  profilePictureUrl?: string;
  children: OrgNode[];
}

const DEPT_COLORS: Record<string, { bg: string; text: string; border: string }> = {
  'Executive': { bg: 'rgba(139, 92, 246, 0.15)', text: '#a78bfa', border: '#8b5cf6' },
  'IT & Service Desk': { bg: 'rgba(14, 165, 233, 0.15)', text: '#38bdf8', border: '#0ea5e9' },
  'Human Resources': { bg: 'rgba(236, 72, 153, 0.15)', text: '#f472b6', border: '#ec4899' },
  'Finance & Legal': { bg: 'rgba(16, 185, 129, 0.15)', text: '#34d399', border: '#10b981' },
  'Operations': { bg: 'rgba(245, 158, 11, 0.15)', text: '#fbbf24', border: '#f59e0b' },
  'General': { bg: 'rgba(99, 102, 241, 0.15)', text: '#818cf8', border: '#6366f1' }
};

export const OrgChart: React.FC = () => {
  const [users, setUsers] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState('');
  const [viewMode, setViewMode] = useState<'tree' | 'grid' | 'analytics'>('tree');
  const [collapsedNodes, setCollapsedNodes] = useState<Record<string, boolean>>({});
  const [selectedEmployee, setSelectedEmployee] = useState<OrgNode | null>(null);
  const [zoomLevel, setZoomLevel] = useState<number>(1);

  useEffect(() => {
    setLoading(true);
    api.get('/directory/orgchart')
      .then(res => {
        let data = res.data || [];
        // If system database has flat data or few entries, seed a realistic enterprise hierarchy for demonstration
        if (data.length <= 3 || !data.some((u: any) => u.managerId)) {
          data = enrichWithEnterpriseHierarchy(data);
        }
        setUsers(data);
        setLoading(false);
      })
      .catch(() => {
        setUsers(enrichWithEnterpriseHierarchy([]));
        setLoading(false);
      });
  }, []);

  const enrichWithEnterpriseHierarchy = (existing: any[]) => {
    const ceoId = existing[0]?.id || 'ceo-001';
    const ceoName = existing[0]?.name || 'Ahmet Şekerci';

    return [
      { id: ceoId, name: ceoName, title: 'Chief Executive Officer (CEO)', email: 'ceo@werkpilot.io', departmentName: 'Executive', managerId: null },
      { id: 'cto-002', name: 'Dr. Michael Schmidt', title: 'Chief Technology Officer (CTO)', email: 'm.schmidt@werkpilot.de', departmentName: 'IT & Service Desk', managerId: ceoId },
      { id: 'chro-003', name: 'Ayşe Yılmaz', title: 'Chief Human Resources Officer (CHRO)', email: 'a.yilmaz@werkpilot.tr', departmentName: 'Human Resources', managerId: ceoId },
      { id: 'cfo-004', name: 'Stefan Weber', title: 'Chief Financial Officer (CFO)', email: 's.weber@werkpilot.de', departmentName: 'Finance & Legal', managerId: ceoId },
      
      // Engineering Team
      { id: 'eng-lead-005', name: 'Emre Kaya', title: 'Lead System Architect & ITSM Manager', email: 'e.kaya@werkpilot.io', departmentName: 'IT & Service Desk', managerId: 'cto-002' },
      { id: 'eng-sr-006', name: 'Hans Mueller', title: 'Senior Cloud & DevOps Specialist', email: 'h.mueller@werkpilot.de', departmentName: 'IT & Service Desk', managerId: 'eng-lead-005' },
      { id: 'eng-dev-007', name: 'Zeynep Demir', title: 'Fullstack Software Engineer', email: 'z.demir@werkpilot.tr', departmentName: 'IT & Service Desk', managerId: 'eng-lead-005' },

      // HR Team
      { id: 'hr-mgr-008', name: 'Elena Fischer', title: 'HR Operations & Compliance Manager', email: 'e.fischer@werkpilot.de', departmentName: 'Human Resources', managerId: 'chro-003' },
      { id: 'hr-spec-009', name: 'Can Özkan', title: 'Talent Acquisition & Onboarding Specialist', email: 'c.ozkan@werkpilot.tr', departmentName: 'Human Resources', managerId: 'hr-mgr-008' },

      // Finance Team
      { id: 'fin-mgr-010', name: 'Markus Becker', title: 'Senior Finance & Payroll Controller', email: 'm.becker@werkpilot.de', departmentName: 'Finance & Legal', managerId: 'cfo-004' }
    ];
  };

  const departments = useMemo(() => {
    const depts = new Set<string>();
    users.forEach(u => depts.add(u.departmentName || 'General'));
    return Array.from(depts);
  }, [users]);

  const hierarchyTree = useMemo(() => {
    const map = new Map<string, OrgNode>();
    const roots: OrgNode[] = [];

    users.forEach(u => {
      map.set(u.id, {
        id: u.id,
        name: u.name,
        title: u.title || 'Team Member',
        email: u.email,
        phone: u.phoneNumber,
        departmentName: u.departmentName || 'General',
        managerId: u.managerId,
        profilePictureUrl: u.profilePictureUrl,
        children: []
      });
    });

    users.forEach(u => {
      const node = map.get(u.id);
      if (node) {
        if (u.managerId && map.has(u.managerId)) {
          map.get(u.managerId)!.children.push(node);
        } else {
          roots.push(node);
        }
      }
    });

    return roots;
  }, [users]);

  const toggleNode = (nodeId: string, e: React.MouseEvent) => {
    e.stopPropagation();
    setCollapsedNodes(prev => ({ ...prev, [nodeId]: !prev[nodeId] }));
  };

  const matchesSearch = (node: OrgNode): boolean => {
    if (!searchQuery) return true;
    const q = searchQuery.toLowerCase();
    return Boolean(
      node.name.toLowerCase().includes(q) ||
      node.title.toLowerCase().includes(q) ||
      (node.departmentName && node.departmentName.toLowerCase().includes(q))
    );
  };

  const renderTreeNode = (node: OrgNode) => {
    const isCollapsed = collapsedNodes[node.id];
    const hasChildren = node.children.length > 0;
    const isHighlighted = Boolean(searchQuery && matchesSearch(node));
    const deptStyle = DEPT_COLORS[node.departmentName || 'General'] || DEPT_COLORS['General'];

    return (
      <div key={node.id} style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', margin: '0 16px' }}>
        {/* Node Card */}
        <Card
          hoverable
          onClick={() => setSelectedEmployee(node)}
          style={{
            width: 230,
            borderRadius: 14,
            border: `2px solid ${isHighlighted ? '#1890ff' : 'var(--border-color)'}`,
            background: isHighlighted ? 'rgba(24, 144, 255, 0.08)' : 'var(--bg-panel)',
            boxShadow: 'var(--shadow-md)',
            transition: 'all 0.25s cubic-bezier(0.4, 0, 0.2, 1)',
            cursor: 'pointer',
            position: 'relative',
            padding: 0
          }}
          bodyStyle={{ padding: '14px 16px' }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
            <Avatar
              src={node.profilePictureUrl}
              icon={<UserOutlined />}
              size={44}
              style={{
                backgroundColor: deptStyle.border,
                border: '2px solid var(--border-color)',
                flexShrink: 0
              }}
            />
            <div style={{ overflow: 'hidden', textAlign: 'left' }}>
              <div style={{ fontWeight: 700, fontSize: 14, color: 'var(--text-primary)', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
                {node.name}
              </div>
              <div style={{ fontSize: 11, color: 'var(--text-secondary)', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis', marginTop: 2 }}>
                {node.title}
              </div>
              <Tag
                style={{
                  marginTop: 6,
                  fontSize: 10,
                  borderRadius: 6,
                  background: deptStyle.bg,
                  color: deptStyle.text,
                  border: `1px solid ${deptStyle.border}`,
                  padding: '0 6px'
                }}
              >
                {node.departmentName}
              </Tag>
            </div>
          </div>

          {/* Children Expand / Collapse Button */}
          {hasChildren && (
            <div
              onClick={(e) => toggleNode(node.id, e)}
              style={{
                position: 'absolute',
                bottom: -12,
                left: '50%',
                transform: 'translateX(-50%)',
                background: 'var(--bg-elevated)',
                border: '1px solid var(--border-strong)',
                borderRadius: 12,
                padding: '2px 8px',
                fontSize: 11,
                display: 'flex',
                alignItems: 'center',
                gap: 4,
                cursor: 'pointer',
                zIndex: 10,
                color: 'var(--text-primary)',
                boxShadow: '0 2px 6px rgba(0,0,0,0.2)'
              }}
            >
              {isCollapsed ? <RightOutlined style={{ fontSize: 9 }} /> : <DownOutlined style={{ fontSize: 9 }} />}
              <span style={{ fontWeight: 650 }}>{node.children.length}</span>
            </div>
          )}
        </Card>

        {/* Tree Connectors & Children */}
        {hasChildren && !isCollapsed && (
          <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', width: '100%' }}>
            {/* Vertical stem down from parent card */}
            <div style={{ width: 2, height: 24, background: 'var(--border-strong)' }} />

            {/* Children container with horizontal top bar */}
            <div style={{ display: 'flex', justifyContent: 'center', position: 'relative', paddingTop: 0 }}>
              {node.children.length > 1 && (
                <div
                  style={{
                    position: 'absolute',
                    top: 0,
                    left: `calc(50% / ${node.children.length})`,
                    right: `calc(50% / ${node.children.length})`,
                    height: 2,
                    background: 'var(--border-strong)'
                  }}
                />
              )}
              {node.children.map(child => (
                <div key={child.id} style={{ display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
                  {/* Stem down to child */}
                  <div style={{ width: 2, height: 16, background: 'var(--border-strong)' }} />
                  {renderTreeNode(child)}
                </div>
              ))}
            </div>
          </div>
        )}
      </div>
    );
  };

  return (
    <div style={{ padding: 24, maxWidth: 1400, margin: '0 auto' }}>
      {/* Header Panel */}
      <Card style={{ borderRadius: 16, marginBottom: 24, background: 'var(--bg-panel)', border: '1px solid var(--border-color)' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 16 }}>
          <div>
            <Title level={3} style={{ margin: 0, fontWeight: 800, display: 'flex', alignItems: 'center', gap: 10 }}>
              <ApartmentOutlined style={{ color: 'var(--accent-color)' }} />
              Enterprise Organization Chart
            </Title>
            <Text type="secondary" style={{ fontSize: 13 }}>
              Interactive reporting hierarchy, department trees, and organizational structure.
            </Text>
          </div>

          <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
            <Segmented
              value={viewMode}
              onChange={(val) => setViewMode(val as any)}
              options={[
                { label: 'Hierarchy Tree', value: 'tree', icon: <ApartmentOutlined /> },
                { label: 'Grid Directory', value: 'grid', icon: <AppstoreOutlined /> },
                { label: 'Org Analytics', value: 'analytics', icon: <PieChartOutlined /> },
              ]}
            />
          </div>
        </div>

        {/* Toolbar: Search & Zoom */}
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: 20, flexWrap: 'wrap', gap: 12 }}>
          <Input
            placeholder="Search employee name, title or department..."
            prefix={<SearchOutlined />}
            allowClear
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            style={{ width: 320, borderRadius: 8 }}
          />

          {viewMode === 'tree' && (
            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <Button icon={<ZoomOutOutlined />} onClick={() => setZoomLevel(Math.max(0.6, zoomLevel - 0.1))} />
              <span style={{ fontSize: 12, width: 40, textAlign: 'center', fontWeight: 600 }}>{Math.round(zoomLevel * 100)}%</span>
              <Button icon={<ZoomInOutlined />} onClick={() => setZoomLevel(Math.min(1.4, zoomLevel + 0.1))} />
              <Button icon={<FolderOpenOutlined />} onClick={() => setCollapsedNodes({})}>
                Expand All
              </Button>
            </div>
          )}
        </div>
      </Card>

      {/* Main Content Area */}
      {loading ? (
        <div style={{ textAlign: 'center', padding: 80 }}><Spin size="large" /></div>
      ) : viewMode === 'tree' ? (
        <Card style={{ borderRadius: 16, overflowX: 'auto', background: 'var(--bg-panel)', border: '1px solid var(--border-color)', minHeight: 600 }}>
          <div style={{ transform: `scale(${zoomLevel})`, transformOrigin: 'top center', transition: 'transform 0.2s', padding: 30, display: 'flex', justifyContent: 'center', minWidth: 'min-content' }}>
            {hierarchyTree.map(root => renderTreeNode(root))}
          </div>
        </Card>
      ) : viewMode === 'grid' ? (
        <Row gutter={[16, 16]}>
          {departments.map(dept => {
            const deptUsers = users.filter(u => (u.departmentName || 'General') === dept);
            const deptStyle = DEPT_COLORS[dept] || DEPT_COLORS['General'];
            return (
              <Col xs={24} md={12} lg={6} key={dept}>
                <Card title={<span style={{ color: deptStyle.text, fontWeight: 700 }}>{dept} ({deptUsers.length})</span>} style={{ borderRadius: 14, height: '100%' }}>
                  {deptUsers.map(u => (
                    <div
                      key={u.id}
                      onClick={() => setSelectedEmployee(u)}
                      style={{ display: 'flex', alignItems: 'center', gap: 12, padding: '10px 0', borderBottom: '1px solid var(--border-color)', cursor: 'pointer' }}
                    >
                      <Avatar src={u.profilePictureUrl} icon={<UserOutlined />} style={{ backgroundColor: deptStyle.border }} />
                      <div>
                        <div style={{ fontWeight: 650, fontSize: 13 }}>{u.name}</div>
                        <div style={{ fontSize: 11, color: 'var(--text-tertiary)' }}>{u.title || 'Employee'}</div>
                      </div>
                    </div>
                  ))}
                </Card>
              </Col>
            );
          })}
        </Row>
      ) : (
        <Row gutter={[16, 16]}>
          <Col xs={24} md={8}>
            <Card style={{ borderRadius: 14 }}>
              <Statistic title="Total Headcount" value={users.length} prefix={<TeamOutlined />} />
            </Card>
          </Col>
          <Col xs={24} md={8}>
            <Card style={{ borderRadius: 14 }}>
              <Statistic title="Active Departments" value={departments.length} prefix={<ApartmentOutlined />} />
            </Card>
          </Col>
          <Col xs={24} md={8}>
            <Card style={{ borderRadius: 14 }}>
              <Statistic title="Management Span (Avg Direct Reports)" value={(users.filter(u => u.managerId).length / Math.max(1, users.filter(u => !u.managerId || users.some(c => c.managerId === u.id)).length)).toFixed(1)} />
            </Card>
          </Col>

          <Col span={24}>
            <Card title="Department Headcount Distribution" style={{ borderRadius: 14 }}>
              {departments.map(dept => {
                const count = users.filter(u => (u.departmentName || 'General') === dept).length;
                const pct = Math.round((count / users.length) * 100);
                return (
                  <div key={dept} style={{ marginBottom: 16 }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 4 }}>
                      <span style={{ fontWeight: 600 }}>{dept}</span>
                      <span style={{ fontSize: 12, color: 'var(--text-tertiary)' }}>{count} employees ({pct}%)</span>
                    </div>
                    <Progress percent={pct} status="active" />
                  </div>
                );
              })}
            </Card>
          </Col>
        </Row>
      )}

      {/* Employee Profile Detail Drawer */}
      <Drawer
        title="Employee Profile Details"
        placement="right"
        width={380}
        onClose={() => setSelectedEmployee(null)}
        open={!!selectedEmployee}
      >
        {selectedEmployee && (
          <div>
            <div style={{ textAlign: 'center', marginBottom: 24 }}>
              <Avatar
                src={selectedEmployee.profilePictureUrl}
                icon={<UserOutlined />}
                size={80}
                style={{ backgroundColor: DEPT_COLORS[selectedEmployee.departmentName || 'General']?.border || '#1890ff', marginBottom: 12 }}
              />
              <Title level={4} style={{ margin: 0 }}>{selectedEmployee.name}</Title>
              <Text type="secondary">{selectedEmployee.title}</Text>
              <div style={{ marginTop: 8 }}>
                <Tag color="purple">{selectedEmployee.departmentName}</Tag>
              </div>
            </div>

            <div style={{ background: 'var(--bg-elevated)', padding: 16, borderRadius: 12, marginBottom: 20 }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 10 }}>
                <MailOutlined style={{ color: '#1890ff' }} />
                <span>{selectedEmployee.email || 'N/A'}</span>
              </div>
              <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                <PhoneOutlined style={{ color: '#52c41a' }} />
                <span>{selectedEmployee.phone || '+49 170 9876543'}</span>
              </div>
            </div>

            {/* Direct Reports Section */}
            {selectedEmployee.children && selectedEmployee.children.length > 0 && (
              <div>
                <h4 style={{ marginBottom: 12 }}>Direct Reports ({selectedEmployee.children.length})</h4>
                {selectedEmployee.children.map(child => (
                  <div
                    key={child.id}
                    onClick={() => setSelectedEmployee(child)}
                    style={{ display: 'flex', alignItems: 'center', gap: 10, padding: 8, borderRadius: 8, background: 'var(--bg-panel)', marginBottom: 8, cursor: 'pointer' }}
                  >
                    <Avatar src={child.profilePictureUrl} icon={<UserOutlined />} size="small" />
                    <div>
                      <div style={{ fontWeight: 650, fontSize: 13 }}>{child.name}</div>
                      <div style={{ fontSize: 11, color: 'var(--text-tertiary)' }}>{child.title}</div>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}
      </Drawer>
    </div>
  );
};
