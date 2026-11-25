---
status: accepted
---

# Identify a Physical Monitor by Its Serial Number, or by Its Connection

## Context

ScreenNap restores a blackout on the same physical monitor after a display change
(specification DISP-3, DISP-4) and keeps live blackouts per physical monitor (DISP-2,
INV-2). Windows numbers display device paths (`\\.\DISPLAYn`) by the order in which it
attaches outputs, so a path can move to another monitor after a reconnect; a path does
not identify a monitor.

The identity used before was the EDID manufacturer ID, the EDID product code, and the
connector instance that `DisplayConfigGetDeviceInfo` reports for a display target. The
connector instance numbers connectors only within one output technology, so two monitors
of the same model on, say, an HDMI and a DisplayPort output reported the same identity,
and a restoration could land on the wrong one. The identity was also recorded only when
Windows reported a non-blank friendly name, so a monitor without one was never restored.

The maintainer decided that two monitors of the same model must be told apart.

Windows offers these values for an active display target:

- The monitor's EDID, which the display driver stores under the monitor's device node
  (`Device Parameters\EDID` in the registry). It carries the manufacturer ID, the
  product code, a 32-bit serial number, and optionally a serial-number text descriptor.
  Many monitors fill in a serial number; some leave it zero or empty
- The monitor's device interface path (`monitorDevicePath` of
  `DISPLAYCONFIG_TARGET_DEVICE_NAME`). Its device instance combines the monitor's
  hardware ID with the adapter output it is attached to, so it is distinct for every
  connection and stable while the monitor stays on that connection

## Decision

- A monitor whose EDID carries a serial number is identified by its EDID manufacturer
  ID, product code, and serial number. The serial number is the serial-number text
  descriptor when the EDID carries one, and otherwise the 32-bit serial number when it is
  not zero
- A monitor whose EDID carries no serial number, or whose EDID cannot be read, is
  identified by its device interface path
- The identity is taken whenever Windows reports the display target, independently of
  whether Windows also reports a friendly name

## Consequences

- Two monitors of the same model are told apart: by their serial numbers, or by their
  connections when they have none
- A monitor with a serial number keeps its identity when it moves to another connection,
  so its blackout is restored there; a monitor without one is a new monitor on another
  connection, and its blackout is not restored there
- Two monitors that report the same nonzero serial number and model cannot be told apart
- Reading the EDID adds one configuration-manager call and one registry read per monitor
  to each enumeration

## Alternatives considered

- **The device interface path alone**: tells every connection apart and needs no EDID,
  but a monitor moved to another connection would lose its pending restoration even when
  its EDID identifies it
- **The adapter LUID and target ID**: the LUID changes when the display driver restarts,
  which can happen while ScreenNap runs

## Revisit when

- Monitors in use report duplicate serial numbers, or Windows stops storing the EDID under
  the monitor's device node
