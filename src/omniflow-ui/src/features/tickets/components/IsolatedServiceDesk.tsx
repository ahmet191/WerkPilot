import React from 'react';
import { useParams } from 'react-router-dom';
import { DynamicPage } from '../../../shared/components/dynamic/DynamicPage';

export const IsolatedServiceDesk: React.FC = () => {
    // TODO: scope the grid to this desk once the backend supports per-desk ticket schemas.
    useParams<{ id: string }>();
    
    // We can render a customized DynamicPage or our own grid here.
    // For now, let's reuse DynamicPage but pass the isolated endpoint to it!
    // Since DynamicPage fetches schema from /api/uischema/{entityType}, 
    // we need to tell the UiSchemaController that we are viewing "tickets" but for a specific service desk.
    
    return (
        <div>
            <h2>Service Desk Area</h2>
            <DynamicPage entityType={`tickets`} />
        </div>
    );
};
