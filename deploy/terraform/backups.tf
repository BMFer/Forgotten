# Off-instance database backups: a private, versioned, encrypted S3 bucket, and an
# IAM role (instance profile) that lets ONLY this EC2 write backups to it — no
# long-lived keys on the box. Lifecycle rules expire old backups automatically.

resource "random_id" "backup_suffix" {
  byte_length = 4
}

resource "aws_s3_bucket" "backups" {
  bucket = "forgotten-nft-backups-${random_id.backup_suffix.hex}"
  tags   = { Project = "forgotten" }
}

resource "aws_s3_bucket_public_access_block" "backups" {
  bucket                  = aws_s3_bucket.backups.id
  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_versioning" "backups" {
  bucket = aws_s3_bucket.backups.id
  versioning_configuration { status = "Enabled" }
}

resource "aws_s3_bucket_server_side_encryption_configuration" "backups" {
  bucket = aws_s3_bucket.backups.id
  rule {
    apply_server_side_encryption_by_default { sse_algorithm = "AES256" }
  }
}

resource "aws_s3_bucket_lifecycle_configuration" "backups" {
  bucket = aws_s3_bucket.backups.id
  rule {
    id     = "expire-db-backups"
    status = "Enabled"
    filter { prefix = "db/" }
    expiration { days = var.backup_retention_days }
    noncurrent_version_expiration { noncurrent_days = var.backup_retention_days }
  }
}

data "aws_iam_policy_document" "ec2_assume" {
  statement {
    actions = ["sts:AssumeRole"]
    principals {
      type        = "Service"
      identifiers = ["ec2.amazonaws.com"]
    }
  }
}

resource "aws_iam_role" "backup" {
  name               = "forgotten-backup-role"
  assume_role_policy = data.aws_iam_policy_document.ec2_assume.json
  tags               = { Project = "forgotten" }
}

data "aws_iam_policy_document" "backup" {
  statement {
    sid       = "PutBackups"
    actions   = ["s3:PutObject"]
    resources = ["${aws_s3_bucket.backups.arn}/db/*"]
  }
  statement {
    sid       = "ListBackups"
    actions   = ["s3:ListBucket"]
    resources = [aws_s3_bucket.backups.arn]
  }
}

resource "aws_iam_role_policy" "backup" {
  name   = "forgotten-backup-policy"
  role   = aws_iam_role.backup.id
  policy = data.aws_iam_policy_document.backup.json
}

resource "aws_iam_instance_profile" "backup" {
  name = "forgotten-backup-profile"
  role = aws_iam_role.backup.name
}
