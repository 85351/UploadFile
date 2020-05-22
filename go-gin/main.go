package main

import (
	"errors"
	"fmt"
	"os"
	"path"
	"strings"

	"github.com/gin-gonic/gin"
)

func main() {

	r := gin.Default()
	r.GET("/ping", getUser)
	r.POST("/upload", uploadFile)
	r.Run(":8888") // listen and serve on 0.0.0.0:8080
}

func getUser(c *gin.Context) {
	c.JSON(200, gin.H{
		"message": "pong",
	})
}

var container string = "container"

func uploadFile(c *gin.Context) {

	var err error
	var relativePath string

	file, err := c.FormFile("FileData")
	fileName := c.PostForm("FileName")

	token := c.PostForm("Token")
	fmt.Println(token)

	for true {
		if err != nil {
			break
		}
		ext := path.Ext(fileName)
		if strings.ToLower(ext) == ".exe" {
			fileName += ".bak"
		}

		filePath := combine(getCurrentDirectory(), container, fileName)
		relativePath = container + fileName

		dir, _ := path.Split(filePath)
		exist, err := createPathIfNotExists(dir)
		if !exist {
			err = errors.New("目录[" + dir + "]创建失败." + err.Error())
			break
		}

		c.SaveUploadedFile(file, filePath)
		break
	}

	if err == nil {
		c.JSON(200, gin.H{
			"success": true,
			"file":    relativePath,
		})
		return
	}

	c.JSON(500, gin.H{
		"success": false,
		"msg":     err.Error(),
	})

}

func combine(dir string, container string, filename string) string {
	dir = strings.ReplaceAll(dir, "\\", "/")
	filename = strings.ReplaceAll(filename, "\\", "/")
	if strings.LastIndex(dir, "/") != 0 {
		dir += "/"
	}
	if strings.Index(filename, "/") != 0 {
		filename = "/" + filename
	}

	return dir + container + filename
}
func createPathIfNotExists(path string) (bool, error) {
	_, err := os.Stat(path)
	if err == nil {
		return true, nil
	}
	if os.IsExist(err) {
		return true, nil
	}
	err = os.MkdirAll(path, os.ModePerm)
	if err == nil {
		return true, nil
	}
	return false, err
}
