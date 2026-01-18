-- Migration: Add delivered_at column to notifications table
-- Date: 2026-01-18
-- Description: Adds delivered_at timestamp field to track when notifications were successfully delivered

-- Add delivered_at column if it doesn't exist
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'notifications'
          AND column_name = 'delivered_at'
    ) THEN
        ALTER TABLE public.notifications
        ADD COLUMN delivered_at TIMESTAMP WITH TIME ZONE DEFAULT NULL;

        RAISE NOTICE 'Column delivered_at added to notifications table';
    ELSE
        RAISE NOTICE 'Column delivered_at already exists in notifications table';
    END IF;
END $$;

-- Add index for performance on delivered_at queries
CREATE INDEX IF NOT EXISTS idx_notifications_delivered_at
ON public.notifications(delivered_at)
WHERE delivered_at IS NOT NULL;

-- Add index for status queries if not exists
CREATE INDEX IF NOT EXISTS idx_notifications_status
ON public.notifications(status);

-- Add index for requested_at queries if not exists
CREATE INDEX IF NOT EXISTS idx_notifications_requested_at
ON public.notifications(requested_at);

COMMENT ON COLUMN public.notifications.delivered_at IS 'Timestamp when the notification was successfully delivered';
