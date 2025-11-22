# Alpha Vantage Integration - Rollback Procedures

## Table of Contents

1. [Overview](#overview)
2. [Rollback Decision Matrix](#rollback-decision-matrix)
3. [Rollback Types](#rollback-types)
4. [Quick Rollback Procedures](#quick-rollback-procedures)
5. [Full Rollback Procedures](#full-rollback-procedures)
6. [Emergency Rollback](#emergency-rollback)
7. [Post-Rollback Actions](#post-rollback-actions)
8. [Prevention Strategies](#prevention-strategies)

---

## Overview

This document provides detailed procedures for rolling back the Alpha Vantage integration in case of issues. Rollback procedures are designed to minimize downtime and restore service quickly.

### Rollback Philosophy

- **Safety First**: Prioritize system stability over new features
- **Quick Response**: Have procedures ready to execute immediately
- **Minimal Downtime**: Use strategies that minimize service interruption
- **Data Integrity**: Ensure no data loss during rollback
- **Clear Communication**: Keep stakeholders informed

### Rollback Levels

1. **Configuration Rollback** (5 minutes): Switch providers via configuration
2. **Code Rollback** (15 minutes): Restore previous code version
3. **Full Rollback** (30 minutes): Complete system restoration
4. **Emergency Rollback** (2 minutes): Immediate service restoration

---

## Rollback Decision Matrix

### When to Rollback

| Condition | Severity | Action | Timeframe |
|-----------|----------|--------|-----------|
| Error rate > 10% | Critical | Emergency rollback | Immediate |
| Error rate 5-10% | High | Full rollback | Within 15 min |
| Error rate 2-5% | Medium | Configuration rollback | Within 30 min |
| Response time +200% | High | Full rollback | Within 15 min |
| Response time +100% | Medium | Configuration rollback | Within 30 min |
| Data corruption | Critical | Emergency rollback | Immediate |
| Security breach | Critical | Emergency rollback | Immediate |
| All providers down | Critical | Emergency rollback | Immediate |
| Rate limit issues | Low | Configuration rollback | Within 1 hour |
| Cost overrun | Low | Configuration rollback | Within 1 hour |


### Decision Flowchart

```
Issue Detected
    │
    ├─> Error rate > 10%? ──YES──> Emergency Rollback
    │        │
    │        NO
    │        │
    ├─> Error rate > 5%? ──YES──> Full Rollback
    │        │
    │        NO
    │        │
    ├─> Error rate > 2%? ──YES──> Configuration Rollback
    │        │
    │        NO
    │        │
    └─> Monitor and Investigate
```

---

## Rollback Types

### Type 1: Configuration Rollback

**Use When**:
- Alpha Vantage causing issues
- Code is stable
- Need quick provider switch

**Impact**:
- Minimal downtime (< 5 minutes)
- No code changes
- Preserves all data

**Procedure**: See [Quick Rollback Procedures](#quick-rollback-procedures)

### Type 2: Code Rollback

**Use When**:
- New code has bugs
- Configuration rollback insufficient
- Need to restore previous version

**Impact**:
- Moderate downtime (15-30 minutes)
- Restores previous code version
- Preserves all data

**Procedure**: See [Full Rollback Procedures](#full-rollback-procedures)

### Type 3: Database Rollback

**Use When**:
- Database migrations failed
- Data corruption detected
- Schema changes need reverting

**Impact**:
- Significant downtime (30-60 minutes)
- May lose recent data
- Requires database restore

**Procedure**: See [Full Rollback Procedures](#full-rollback-procedures)

### Type 4: Emergency Rollback

**Use When**:
- Critical system failure
- Security breach
- Data corruption
- Complete service outage

**Impact**:
- Immediate action required
- May involve service interruption
- Prioritizes restoration over data

**Procedure**: See [Emergency Rollback](#emergency-rollback)


---

## Quick Rollback Procedures

### Procedure 1: Switch to Yahoo Finance (Configuration Only)

**Estimated Time**: 5 minutes  
**Downtime**: < 1 minute

**Steps**:

```bash
# 1. Connect to server
ssh user@production-server

# 2. Update environment variable
export DataProvider__PrimaryProvider="YahooFinance"
export AlphaVantage__Enabled="false"

# Or update configuration file
sudo nano /var/www/stocksensepro/appsettings.Production.json
# Change:
# "PrimaryProvider": "YahooFinance"
# "AlphaVantage": { "Enabled": false }

# 3. Restart application
sudo systemctl restart stocksensepro

# 4. Verify health
curl http://localhost:5000/api/health

# 5. Verify provider
curl http://localhost:5000/api/health/metrics | jq '.currentProvider'
# Should show "yahooFinance"

# 6. Monitor logs
sudo journalctl -u stocksensepro -f
```

**Verification**:
- [ ] Application started successfully
- [ ] Health check returns "Healthy"
- [ ] Current provider is "yahooFinance"
- [ ] No errors in logs
- [ ] Response times normal
- [ ] Error rate < 1%

### Procedure 2: Disable Alpha Vantage Fallback

**Estimated Time**: 3 minutes  
**Downtime**: < 1 minute

**Steps**:

```bash
# 1. Update configuration
export DataProvider__FallbackProvider=""
export DataProvider__EnableAutomaticFallback="false"

# 2. Restart application
sudo systemctl restart stocksensepro

# 3. Verify
curl http://localhost:5000/api/health/metrics | jq '.strategy'
# Should show "Primary"
```


### Procedure 3: Clear Cache (If Needed)

**Estimated Time**: 2 minutes  
**Downtime**: None

**Steps**:

```bash
# 1. Connect to Redis
redis-cli

# 2. Clear Alpha Vantage cache only
KEYS alphavantage:*
# Review keys, then:
EVAL "return redis.call('del', unpack(redis.call('keys', 'alphavantage:*')))" 0

# Or clear all cache
FLUSHALL

# 3. Verify
KEYS *
# Should show no alphavantage keys

# 4. Monitor cache rebuild
# Application will automatically repopulate cache
```

---

## Full Rollback Procedures

### Procedure 1: Restore from Backup

**Estimated Time**: 15-30 minutes  
**Downtime**: 15-30 minutes

**Prerequisites**:
- Backup exists and is accessible
- Backup is recent (< 24 hours old)
- Backup has been tested

**Steps**:

```bash
# 1. Stop application
sudo systemctl stop stocksensepro

# 2. Backup current state (for investigation)
sudo cp -r /var/www/stocksensepro /var/www/stocksensepro-failed-$(date +%Y%m%d-%H%M%S)

# 3. Remove current deployment
sudo rm -rf /var/www/stocksensepro

# 4. Restore from backup
sudo cp -r /var/www/stocksensepro-backup-YYYYMMDD-HHMMSS /var/www/stocksensepro

# 5. Restore configuration
sudo cp /backup/appsettings.Production.json /var/www/stocksensepro/

# 6. Verify files
ls -la /var/www/stocksensepro

# 7. Start application
sudo systemctl start stocksensepro

# 8. Check status
sudo systemctl status stocksensepro

# 9. Monitor logs
sudo journalctl -u stocksensepro -f

# 10. Verify health
curl http://localhost:5000/api/health

# 11. Test endpoints
curl http://localhost:5000/api/stocks/quote/AAPL
```

**Verification**:
- [ ] Application started successfully
- [ ] All endpoints responding
- [ ] No errors in logs
- [ ] Performance normal
- [ ] Using previous provider configuration


### Procedure 2: Git Revert

**Estimated Time**: 20 minutes  
**Downtime**: 15-20 minutes

**Steps**:

```bash
# 1. Stop application
sudo systemctl stop stocksensepro

# 2. Navigate to repository
cd /var/www/stocksensepro

# 3. Check current commit
git log --oneline -5

# 4. Identify commit to revert to
# Find the last known good commit

# 5. Revert to previous commit
git checkout <commit-hash>

# Or revert to previous tag
git checkout v1.0.0

# 6. Rebuild application
dotnet build --configuration Release

# 7. Publish application
dotnet publish -c Release -o /var/www/stocksensepro/publish

# 8. Start application
sudo systemctl start stocksensepro

# 9. Verify
curl http://localhost:5000/api/health
```

### Procedure 3: Database Rollback

**Estimated Time**: 30-60 minutes  
**Downtime**: 30-60 minutes

**⚠️ WARNING**: This procedure may result in data loss. Use only when necessary.

**Steps**:

```bash
# 1. Stop application
sudo systemctl stop stocksensepro

# 2. Backup current database (for investigation)
pg_dump stocksensepro > /backup/failed-db-$(date +%Y%m%d-%H%M%S).sql

# 3. Verify backup exists
ls -lh /backup/backup-YYYYMMDD-HHMMSS.sql

# 4. Drop current database
psql -c "DROP DATABASE stocksensepro;"

# 5. Create new database
psql -c "CREATE DATABASE stocksensepro;"

# 6. Restore from backup
psql stocksensepro < /backup/backup-YYYYMMDD-HHMMSS.sql

# 7. Verify database
psql stocksensepro -c "SELECT COUNT(*) FROM stocks;"

# 8. Start application
sudo systemctl start stocksensepro

# 9. Verify
curl http://localhost:5000/api/health
```


---

## Emergency Rollback

### When to Use Emergency Rollback

- **Critical system failure**: Complete service outage
- **Security breach**: Unauthorized access or data exposure
- **Data corruption**: Database integrity compromised
- **Cascading failures**: Multiple systems failing

### Emergency Rollback Procedure

**Estimated Time**: 2-5 minutes  
**Priority**: Immediate service restoration

**Steps**:

```bash
# 1. IMMEDIATE: Switch to Yahoo Finance
export DataProvider__PrimaryProvider="YahooFinance"
export AlphaVantage__Enabled="false"
sudo systemctl restart stocksensepro

# 2. Verify service is up
curl http://localhost:5000/api/health

# 3. If still failing, restore from backup
sudo systemctl stop stocksensepro
sudo rm -rf /var/www/stocksensepro
sudo cp -r /var/www/stocksensepro-backup-latest /var/www/stocksensepro
sudo systemctl start stocksensepro

# 4. Notify team immediately
# Send alert to on-call team
# Update status page

# 5. Begin investigation
# Collect logs
# Document timeline
# Identify root cause
```

### Emergency Contacts

**On-Call Team**:
- Primary: [Phone/Email]
- Secondary: [Phone/Email]
- Escalation: [Phone/Email]

**Notification Channels**:
- PagerDuty: [Link]
- Slack: #incidents
- Email: ops@company.com

### Emergency Communication Template

```
INCIDENT ALERT

Severity: [Critical/High/Medium]
System: StockSensePro API
Issue: [Brief description]
Impact: [User impact]
Action Taken: [Rollback performed]
Status: [Investigating/Resolved]
ETA: [Estimated resolution time]

Next Update: [Time]
```


---

## Post-Rollback Actions

### Immediate Actions (0-1 hour)

```bash
# 1. Verify system stability
# Monitor for 15 minutes minimum

# 2. Check all critical endpoints
curl http://localhost:5000/api/health
curl http://localhost:5000/api/stocks/quote/AAPL
curl http://localhost:5000/api/stocks/quotes?symbols=AAPL,MSFT

# 3. Review metrics
curl http://localhost:5000/api/health/metrics

# 4. Monitor error rates
# Should return to < 1%

# 5. Check response times
# Should return to normal (< 500ms)

# 6. Verify cache is working
# Cache hit rate should be > 80%
```

### Short-term Actions (1-24 hours)

1. **Document the Incident**
   - What went wrong
   - When it was detected
   - Impact on users
   - Rollback steps taken
   - Time to resolution

2. **Notify Stakeholders**
   ```
   Subject: Incident Resolution - Alpha Vantage Integration
   
   The issue with Alpha Vantage integration has been resolved.
   
   Timeline:
   - Issue detected: [Time]
   - Rollback initiated: [Time]
   - Service restored: [Time]
   - Total downtime: [Duration]
   
   Impact:
   - [Description of user impact]
   
   Resolution:
   - [Description of rollback performed]
   
   Next Steps:
   - Root cause analysis
   - Fix implementation
   - Re-deployment plan
   ```

3. **Collect Diagnostic Data**
   ```bash
   # Save logs
   sudo journalctl -u stocksensepro --since "2 hours ago" > /tmp/rollback-logs.txt
   
   # Save metrics snapshot
   curl http://localhost:5000/api/health/metrics > /tmp/rollback-metrics.json
   
   # Save configuration
   cp appsettings.Production.json /tmp/rollback-config.json
   ```


### Medium-term Actions (1-7 days)

1. **Root Cause Analysis**
   - Review logs and metrics
   - Identify what went wrong
   - Determine why it wasn't caught earlier
   - Document findings

2. **Create Action Items**
   - Fix the identified issue
   - Add tests to prevent recurrence
   - Update deployment procedures
   - Improve monitoring/alerts

3. **Update Documentation**
   - Document the incident
   - Update rollback procedures if needed
   - Add to troubleshooting guide
   - Share learnings with team

4. **Plan Re-deployment**
   - Fix the issue
   - Test thoroughly in staging
   - Schedule new deployment
   - Communicate plan to stakeholders

### Root Cause Analysis Template

```markdown
# Incident Post-Mortem

## Summary
[Brief description of what happened]

## Timeline
- [Time]: Issue detected
- [Time]: Rollback initiated
- [Time]: Service restored
- [Time]: Root cause identified

## Impact
- Users affected: [Number/Percentage]
- Duration: [Time]
- Services impacted: [List]

## Root Cause
[Detailed explanation of what went wrong]

## Contributing Factors
- [Factor 1]
- [Factor 2]

## Resolution
[How the issue was resolved]

## Action Items
- [ ] [Action 1] - Owner: [Name] - Due: [Date]
- [ ] [Action 2] - Owner: [Name] - Due: [Date]

## Lessons Learned
- [Lesson 1]
- [Lesson 2]

## Prevention
[How to prevent this in the future]
```


---

## Prevention Strategies

### Pre-Deployment Prevention

1. **Thorough Testing**
   - Run all unit tests
   - Run integration tests
   - Perform load testing
   - Test rollback procedures

2. **Staging Validation**
   - Deploy to staging first
   - Monitor for 24+ hours
   - Test all scenarios
   - Verify metrics

3. **Gradual Rollout**
   - Start with small percentage of traffic
   - Monitor closely
   - Increase gradually
   - Have rollback ready

4. **Configuration Validation**
   - Validate all configuration
   - Test with production-like data
   - Verify API keys
   - Check rate limits

### Monitoring and Alerts

1. **Real-time Monitoring**
   - Error rate alerts
   - Response time alerts
   - Health check monitoring
   - Cost tracking alerts

2. **Automated Alerts**
   ```yaml
   alerts:
     - name: "High Error Rate"
       condition: "error_rate > 2%"
       action: "Notify on-call team"
       
     - name: "Service Down"
       condition: "health_status != 'Healthy'"
       action: "Page on-call team"
       
     - name: "Performance Degradation"
       condition: "p95_response_time > 1000ms"
       action: "Notify team"
   ```

3. **Dashboard Monitoring**
   - Real-time metrics dashboard
   - Historical trends
   - Provider comparison
   - Cost tracking

### Backup Strategy

1. **Automated Backups**
   ```bash
   # Daily backup script
   #!/bin/bash
   DATE=$(date +%Y%m%d-%H%M%S)
   
   # Backup code
   cp -r /var/www/stocksensepro /backup/code-$DATE
   
   # Backup database
   pg_dump stocksensepro > /backup/db-$DATE.sql
   
   # Backup Redis
   redis-cli SAVE
   cp /var/lib/redis/dump.rdb /backup/redis-$DATE.rdb
   
   # Backup configuration
   cp /var/www/stocksensepro/appsettings.Production.json /backup/config-$DATE.json
   
   # Cleanup old backups (keep 30 days)
   find /backup -name "code-*" -mtime +30 -delete
   find /backup -name "db-*" -mtime +30 -delete
   ```

2. **Backup Verification**
   - Test restore monthly
   - Verify backup integrity
   - Document restore procedures
   - Train team on restoration


### Blue-Green Deployment

**Recommended for production**:

```
┌─────────────────────────────────────────────────────────┐
│                    Load Balancer                         │
└────────────┬────────────────────────────────────────────┘
             │
             ├──> Blue Environment (Current Version)
             │    - Serving 100% traffic
             │    - Stable and tested
             │
             └──> Green Environment (New Version)
                  - Serving 0% traffic initially
                  - Gradually increase to 100%
                  - Instant rollback by switching traffic
```

**Benefits**:
- Zero-downtime deployment
- Instant rollback (just switch traffic)
- Test new version with real traffic
- Keep old version running as backup

### Feature Flags

**Use feature flags for gradual enablement**:

```json
{
  "FeatureFlags": {
    "AlphaVantageEnabled": true,
    "AlphaVantageTrafficPercentage": 10,
    "EnableDataEnrichment": false,
    "EnableCostTracking": true
  }
}
```

**Benefits**:
- Enable features gradually
- Disable features instantly
- A/B testing capability
- No code deployment needed

---

## Rollback Checklist

### Pre-Rollback

- [ ] Issue severity assessed
- [ ] Rollback type determined
- [ ] Team notified
- [ ] Backup verified
- [ ] Rollback procedure reviewed

### During Rollback

- [ ] Application stopped (if needed)
- [ ] Rollback executed
- [ ] Application started
- [ ] Health check verified
- [ ] Endpoints tested
- [ ] Logs reviewed

### Post-Rollback

- [ ] System stable for 15+ minutes
- [ ] Error rate < 1%
- [ ] Response times normal
- [ ] Stakeholders notified
- [ ] Incident documented
- [ ] Root cause analysis scheduled

---

## Quick Reference

### Emergency Commands

```bash
# Quick switch to Yahoo Finance
export DataProvider__PrimaryProvider="YahooFinance"
sudo systemctl restart stocksensepro

# Restore from backup
sudo systemctl stop stocksensepro
sudo rm -rf /var/www/stocksensepro
sudo cp -r /var/www/stocksensepro-backup-latest /var/www/stocksensepro
sudo systemctl start stocksensepro

# Check health
curl http://localhost:5000/api/health

# Check logs
sudo journalctl -u stocksensepro -f
```

### Contact Information

**Emergency Contacts**:
- On-Call: [Phone]
- DevOps Lead: [Phone]
- Engineering Manager: [Phone]

**Escalation Path**:
1. On-Call Engineer
2. DevOps Lead
3. Engineering Manager
4. CTO

---

**Last Updated**: November 22, 2025  
**Version**: 1.0  
**Review Frequency**: Monthly

